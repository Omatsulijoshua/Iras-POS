const express = require("express");
const fs = require("fs");
const path = require("path");

const app = express();
const port = process.env.PORT || 3000;
const username = process.env.ADMIN_USERNAME || "admin";
const password = process.env.ADMIN_PASSWORD || "admin";
const dataFile = process.env.DATA_FILE || path.join(process.env.VERCEL ? "/tmp" : __dirname, "data", "reports.json");

app.use(express.json({ limit: "25mb" }));
app.use(express.urlencoded({ extended: false }));

function ensureDataDir() {
  fs.mkdirSync(path.dirname(dataFile), { recursive: true });
}

function readData() {
  try {
    const data = JSON.parse(fs.readFileSync(dataFile, "utf8"));
    if (!Array.isArray(data.snapshots)) data.snapshots = [];
    if (!Array.isArray(data.users) || data.users.length === 0) data.users = [{ username, password }];
    return data;
  } catch {
    return { snapshots: [], users: [{ username, password }] };
  }
}

function writeData(data) {
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

function currentUser(req) {
  const cookies = parseCookies(req);
  const data = readData();
  return data.users.find((user) => cookies.posales_auth === authToken(user));
}

function requireLogin(req, res, next) {
  if (currentUser(req)) return next();
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

app.post("/login", (req, res) => {
  const data = readData();
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

app.post("/api/upload", (req, res) => {
  const snapshot = req.body || {};
  const data = readData();
  const systemName = systemNameFor(snapshot);
  snapshot.systemName = systemName;
  snapshot.receivedAt = new Date().toISOString();
  data.snapshots = data.snapshots.filter((item) => systemNameFor(item) !== systemName);
  data.snapshots.unshift(snapshot);
  data.snapshots = data.snapshots.slice(0, 100);
  writeData(data);
  res.json({ ok: true, receivedAt: snapshot.receivedAt });
});

app.get("/api/reports", requireLogin, (req, res) => {
  const data = readData();
  res.json(aggregateReports(data));
});

app.get("/api/admin/users", requireLogin, (req, res) => {
  const data = readData();
  res.json({ users: data.users.map((user) => ({ username: user.username })) });
});

app.post("/api/admin/users", requireLogin, (req, res) => {
  const data = readData();
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
  writeData(data);
  res.json({ ok: true });
});

app.post("/api/admin/password", requireLogin, (req, res) => {
  const user = currentUser(req);
  const data = readData();
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
  writeData(data);
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
