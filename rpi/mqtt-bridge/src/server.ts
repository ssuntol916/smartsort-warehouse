import Aedes, { AedesOptions } from "aedes";
import { createServer as createTcpServer, Server as NetServer } from "net";
import { createServer as createHttpServer } from "http";
import express from "express";
import ws from "websocket-stream";
import path from "path";

// ── 설정 ──────────────────────────────────────────────
const MQTT_TCP_PORT = parseInt(process.env.MQTT_PORT || "1883");
const HTTP_PORT = parseInt(process.env.HTTP_PORT || "3000");
const WS_MQTT_PORT = parseInt(process.env.WS_MQTT_PORT || "8083");

// ── Aedes MQTT 브로커 ─────────────────────────────────
const aedesOpts: AedesOptions = {
  id: "smartsort-broker",
  heartbeatInterval: 60000,
  connectTimeout: 30000,
};

const broker = new Aedes(aedesOpts);

// TCP MQTT (1883) - ESP32/장비 연결용
const tcpServer: NetServer = createTcpServer(broker.handle);
tcpServer.listen(MQTT_TCP_PORT, () => {
  console.log(`🔌 MQTT TCP 브로커 실행 중: mqtt://0.0.0.0:${MQTT_TCP_PORT}`);
});

// WebSocket MQTT (8083) - 브라우저 연결용
const wsHttpServer = createHttpServer();

ws.createServer({ server: wsHttpServer }, broker.handle as any);

wsHttpServer.listen(WS_MQTT_PORT, () => {
  console.log(`🌐 MQTT WebSocket 실행 중: ws://0.0.0.0:${WS_MQTT_PORT}`);
});

// ── Express 웹서버 (3000) ─────────────────────────────
const app = express();
app.use(express.json());
app.use(express.static(path.join(__dirname, "..", "public")));

// REST API: 브로커 상태
app.get("/api/stats", (_req, res) => {
  const clients: string[] = [];
  for (const [id] of (broker as any).clients) {
    clients.push(id);
  }
  res.json({
    connectedClients: clients.length,
    clients,
    uptime: process.uptime(),
    memoryUsage: process.memoryUsage(),
  });
});

// REST API: HTTP로 MQTT 발행
app.post("/api/publish", (req, res) => {
  const { topic, message, qos = 0, retain = false } = req.body;

  if (!topic || message === undefined) {
    return res.status(400).json({ error: "topic과 message가 필요합니다." });
  }

  broker.publish(
    {
      topic,
      payload: Buffer.from(String(message)),
      qos: qos as 0 | 1 | 2,
      retain,
      cmd: "publish",
      dup: false,
    },
    (err) => {
      if (err) {
        return res.status(500).json({ error: err.message });
      }
      res.json({ success: true, topic, message });
    }
  );
});

app.listen(HTTP_PORT, "0.0.0.0", () => {
  console.log(`📊 웹 대시보드 실행 중: http://0.0.0.0:${HTTP_PORT}`);
  console.log("─".repeat(50));
  console.log("  SmartSort MQTT 웹 대시보드가 준비되었습니다!");
  console.log("─".repeat(50));
});

// ── 브로커 이벤트 로깅 ────────────────────────────────
broker.on("client", (client) => {
  console.log(`✅ 클라이언트 연결: ${client.id}`);
});

broker.on("clientDisconnect", (client) => {
  console.log(`❌ 클라이언트 해제: ${client.id}`);
});

broker.on("publish", (packet, _client) => {
  if (!packet.topic.startsWith("$SYS")) {
    console.log(
      `📨 [${packet.topic}] ${packet.payload.toString().substring(0, 100)}`
    );
  }
});

broker.on("subscribe", (subscriptions, client) => {
  const topics = subscriptions.map((s) => s.topic).join(", ");
  console.log(`📡 ${client.id} 구독: ${topics}`);
});

// ── 종료 처리 ─────────────────────────────────────────
const shutdown = () => {
  console.log("\n🛑 서버 종료 중...");
  broker.close(() => {
    tcpServer.close();
    wsHttpServer.close();
    process.exit(0);
  });
};

process.on("SIGINT", shutdown);
process.on("SIGTERM", shutdown);
