const express = require("express");
const fs = require("fs");
const path = require("path");
const { Pool } = require("pg");

let pool;
if (process.env.DATABASE_URL) {
  console.log("PostgreSQL database URL configured. Setting up connections...");
  pool = new Pool({
    connectionString: process.env.DATABASE_URL,
    ssl: {
      rejectUnauthorized: false
    }
  });

  initDatabase().catch(err => console.error("Failed to initialize PostgreSQL database:", err));
}

async function initDatabase() {
  const client = await pool.connect();
  try {
    await client.query(`
      CREATE TABLE IF NOT EXISTS posales_reports (
        id INT PRIMARY KEY,
        data JSONB NOT NULL
      );
    `);
    
    const res = await client.query("SELECT id FROM posales_reports WHERE id = 1");
    if (res.rows.length === 0) {
      await client.query(
        "INSERT INTO posales_reports (id, data) VALUES (1, $1)",
        [JSON.stringify({ snapshots: [], users: [{ username: "admin", password: "admin" }] })]
      );
      console.log("Initialized PostgreSQL table with default admin user.");
    }
  } finally {
    client.release();
  }
}

const app = express();
const port = process.env.PORT || 3000;
const username = process.env.ADMIN_USERNAME || "admin";
const password = process.env.ADMIN_PASSWORD || "admin";
const dataFile = process.env.DATA_FILE || path.join(process.env.VERCEL ? "/tmp" : __dirname, "data", "reports.json");

app.use(express.json({ limit: "25mb" }));
app.use(express.urlencoded({ extended: false }));

const RENDER_BACKEND_URL = process.env.RENDER_BACKEND_URL;

if (RENDER_BACKEND_URL) {
  console.log(`Configured to proxy API requests to Render backend: ${RENDER_BACKEND_URL}`);
  
  const proxyToRender = async (req, res) => {
    try {
      const targetUrl = RENDER_BACKEND_URL.replace(/\/$/, "") + req.originalUrl;
      const headers = {};
      for (const [key, value] of Object.entries(req.headers)) {
        const lowerKey = key.toLowerCase();
        if (lowerKey !== "host" && lowerKey !== "content-length" && lowerKey !== "connection") {
          headers[key] = value;
        }
      }
      
      const fetchOptions = {
        method: req.method,
        headers: headers,
        redirect: "manual"
      };

      if (req.method !== "GET" && req.method !== "HEAD" && req.body) {
        const contentType = req.headers["content-type"] || "";
        if (contentType.includes("application/json")) {
          fetchOptions.body = JSON.stringify(req.body);
        } else if (contentType.includes("application/x-www-form-urlencoded")) {
          fetchOptions.body = new URLSearchParams(req.body).toString();
        } else {
          fetchOptions.body = typeof req.body === "string" ? req.body : JSON.stringify(req.body);
        }
      }

      const response = await fetch(targetUrl, fetchOptions);
      
      response.headers.forEach((value, key) => {
        const lowerKey = key.toLowerCase();
        if (
          lowerKey !== "content-encoding" &&
          lowerKey !== "transfer-encoding" &&
          lowerKey !== "connection" &&
          lowerKey !== "content-length"
        ) {
          res.setHeader(key, value);
        }
      });
      
      res.status(response.status);
      const buffer = await response.arrayBuffer();
      res.send(Buffer.from(buffer));
    } catch (error) {
      console.error("Proxy to Render error:", error);
      res.status(502).json({ ok: false, error: "Bad Gateway: Failed to connect to Render backend." });
    }
  };

  app.post("/login", proxyToRender);
  app.post("/logout", proxyToRender);
  app.use("/api", proxyToRender);
}

function ensureDataDir() {
  fs.mkdirSync(path.dirname(dataFile), { recursive: true });
}

async function readData() {
  if (pool) {
    try {
      const res = await pool.query("SELECT data FROM posales_reports WHERE id = 1");
      if (res.rows.length > 0) {
        const parsed = res.rows[0].data;
        if (!Array.isArray(parsed.snapshots)) parsed.snapshots = [];
        if (!Array.isArray(parsed.users) || parsed.users.length === 0) parsed.users = [{ username, password }];
        return parsed;
      }
    } catch (e) {
      console.error("Failed to read from PostgreSQL:", e);
    }
  }

  const url = process.env.KV_REST_API_URL;
  const token = process.env.KV_REST_API_TOKEN;
  
  if (url && token) {
    try {
      const response = await fetch(`${url}/get/reports_data`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (response.ok) {
        const json = await response.json();
        if (json && json.result) {
          const parsed = typeof json.result === 'string' ? JSON.parse(json.result) : json.result;
          if (!Array.isArray(parsed.snapshots)) parsed.snapshots = [];
          if (!Array.isArray(parsed.users) || parsed.users.length === 0) parsed.users = [{ username, password }];
          return parsed;
        }
      }
    } catch (e) {
      console.error("Failed to read from Vercel KV:", e);
    }
  }

  // Fallback to local file
  try {
    const data = JSON.parse(fs.readFileSync(dataFile, "utf8"));
    if (!Array.isArray(data.snapshots)) data.snapshots = [];
    if (!Array.isArray(data.users) || data.users.length === 0) data.users = [{ username, password }];
    return data;
  } catch {
    return { snapshots: [], users: [{ username, password }] };
  }
}

async function writeData(data) {
  if (pool) {
    try {
      await pool.query(
        "INSERT INTO posales_reports (id, data) VALUES (1, $1) ON CONFLICT (id) DO UPDATE SET data = EXCLUDED.data",
        [JSON.stringify(data)]
      );
      return;
    } catch (e) {
      console.error("Failed to write to PostgreSQL:", e);
    }
  }

  const url = process.env.KV_REST_API_URL;
  const token = process.env.KV_REST_API_TOKEN;

  if (url && token) {
    try {
      const response = await fetch(url, {
        method: "POST",
        headers: { 
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify(["SET", "reports_data", JSON.stringify(data)])
      });
      if (response.ok) {
        return;
      }
    } catch (e) {
      console.error("Failed to write to Vercel KV:", e);
    }
  }

  // Fallback to local file
  ensureDataDir();
  fs.writeFileSync(dataFile, JSON.stringify(data, null, 2));
}

function parseCookies(req) {
  return Object.fromEntries((req.headers.cookie || "").split(";").filter(Boolean).map((part) => {
    const index = part.indexOf("=");
    return [part.slice(0, index).trim(), decodeURIComponent(part.slice(index + 1))];
  }));
}

function authToken(user) {
  return Buffer.from(`${user.username}:${user.password}`).toString("base64");
}

async function currentUser(req) {
  const cookies = parseCookies(req);
  const token = cookies.posales_auth;
  if (!token) return null;

  if (process.env.RENDER_BACKEND_URL) {
    try {
      const decoded = Buffer.from(token, "base64").toString("utf8");
      if (decoded && decoded.includes(":")) {
        return { username: decoded.split(":")[0] };
      }
    } catch {
      return null;
    }
  }

  const data = await readData();
  return data.users.find((user) => cookies.posales_auth === authToken(user));
}

async function requireLogin(req, res, next) {
  const user = await currentUser(req);
  if (user) return next();
  if (req.path.startsWith("/api/")) {
    res.status(401).json({ ok: false, error: "Not logged in" });
    return;
  }
  res.redirect("/login");
}

function systemNameFor(snapshot) {
  return String(snapshot.systemName || snapshot.store?.system_name || snapshot.store?.store || "Unknown System").trim() || "Unknown System";
}

function withSystem(rows, snapshot) {
  const systemName = systemNameFor(snapshot);
  const storeName = snapshot.store?.store || "";
  return (rows || []).map((row) => ({ ...row, systemName, storeName }));
}

function aggregateReports(data) {
  const snapshots = data.snapshots || [];
  const systems = snapshots.map((snapshot) => {
    const sales = snapshot.sales || [];
    const inventory = snapshot.inventory || [];
    return {
      systemName: systemNameFor(snapshot),
      storeName: snapshot.store?.store || "",
      receivedAt: snapshot.receivedAt || "",
      generatedAt: snapshot.generatedAt || "",
      productCount: inventory.length,
      salesCount: sales.length
    };
  });

  return {
    generatedAt: snapshots[0]?.generatedAt || "",
    receivedAt: snapshots[0]?.receivedAt || "",
    uploadedBy: snapshots[0]?.uploadedBy || "",
    systems,
    store: snapshots[0]?.store || {},
    sales: snapshots.flatMap((snapshot) => withSystem(snapshot.sales, snapshot)),
    unsettled: snapshots.flatMap((snapshot) => withSystem(snapshot.unsettled, snapshot)),
    inventory: snapshots.flatMap((snapshot) => withSystem(snapshot.inventory, snapshot)),
    criticalItems: snapshots.flatMap((snapshot) => withSystem(snapshot.criticalItems, snapshot)),
    cancelled: snapshots.flatMap((snapshot) => withSystem(snapshot.cancelled, snapshot)),
    soldItems: snapshots.flatMap((snapshot) => withSystem(snapshot.soldItems, snapshot)),
    topSelling: snapshots.flatMap((snapshot) => withSystem(snapshot.topSelling, snapshot)),
    stockIn: snapshots.flatMap((snapshot) => withSystem(snapshot.stockIn, snapshot))
  };
}

app.get("/login", (req, res) => {
  res.sendFile(path.join(__dirname, "public", "login.html"));
});

app.post("/login", async (req, res) => {
  const data = await readData();
  const user = data.users.find((item) => item.username === req.body.username && item.password === req.body.password);
  if (user) {
    res.setHeader("Set-Cookie", `posales_auth=${encodeURIComponent(authToken(user))}; Path=/; HttpOnly; SameSite=Lax`);
    res.redirect("/");
    return;
  }
  res.status(401).send("Invalid username or password. <a href=\"/login\">Try again</a>");
});

app.post("/logout", (req, res) => {
  res.setHeader("Set-Cookie", "posales_auth=; Path=/; Max-Age=0");
  res.redirect("/login");
});

app.post("/api/upload", async (req, res) => {
  const snapshot = req.body || {};
  const data = await readData();
  const systemName = systemNameFor(snapshot);
  snapshot.systemName = systemName;
  snapshot.receivedAt = new Date().toISOString();
  data.snapshots = data.snapshots.filter((item) => systemNameFor(item) !== systemName);
  data.snapshots.unshift(snapshot);
  data.snapshots = data.snapshots.slice(0, 100);
  await writeData(data);
  res.json({ ok: true, receivedAt: snapshot.receivedAt });
});

app.get("/api/reports", requireLogin, async (req, res) => {
  const data = await readData();
  res.json(aggregateReports(data));
});

app.get("/api/admin/users", requireLogin, async (req, res) => {
  const data = await readData();
  res.json({ users: data.users.map((user) => ({ username: user.username })) });
});

app.post("/api/admin/users", requireLogin, async (req, res) => {
  const data = await readData();
  const newUsername = String(req.body.username || "").trim();
  const newPassword = String(req.body.password || "").trim();
  if (!newUsername || !newPassword) {
    res.status(400).json({ ok: false, error: "Username and password are required." });
    return;
  }
  if (data.users.some((user) => user.username.toLowerCase() === newUsername.toLowerCase())) {
    res.status(400).json({ ok: false, error: "Admin username already exists." });
    return;
  }
  data.users.push({ username: newUsername, password: newPassword });
  await writeData(data);
  res.json({ ok: true });
});

app.post("/api/admin/password", requireLogin, async (req, res) => {
  const user = await currentUser(req);
  const data = await readData();
  const currentPassword = String(req.body.currentPassword || "");
  const newPassword = String(req.body.newPassword || "").trim();
  if (!newPassword) {
    res.status(400).json({ ok: false, error: "New password is required." });
    return;
  }
  const storedUser = data.users.find((item) => item.username === user.username);
  if (!storedUser || storedUser.password !== currentPassword) {
    res.status(400).json({ ok: false, error: "Current password is incorrect." });
    return;
  }
  storedUser.password = newPassword;
  await writeData(data);
  res.setHeader("Set-Cookie", `posales_auth=${encodeURIComponent(authToken(storedUser))}; Path=/; HttpOnly; SameSite=Lax`);
  res.json({ ok: true });
});

app.get("/", requireLogin, (req, res) => {
  res.sendFile(path.join(__dirname, "public", "index.html"));
});

app.use("/public", express.static(path.join(__dirname, "public")));

if (require.main === module) {
  app.listen(port, () => {
    console.log(`POSales cloud report site running on port ${port}`);
  });
}

module.exports = app;
