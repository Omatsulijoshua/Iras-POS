const express = require("express");
const fs = require("fs");
const path = require("path");

const app = express();
const port = process.env.PORT || 3000;
const username = process.env.ADMIN_USERNAME || "admin";
const password = process.env.ADMIN_PASSWORD || "admin";
const authCookie = Buffer.from(`${username}:${password}`).toString("base64");
const dataFile = process.env.DATA_FILE || path.join(process.env.VERCEL ? "/tmp" : __dirname, "data", "reports.json");

app.use(express.json({ limit: "25mb" }));
app.use(express.urlencoded({ extended: false }));

function ensureDataDir() {
  fs.mkdirSync(path.dirname(dataFile), { recursive: true });
}

function readData() {
  try {
    return JSON.parse(fs.readFileSync(dataFile, "utf8"));
  } catch {
    return { snapshots: [] };
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

function requireLogin(req, res, next) {
  const cookies = parseCookies(req);
  if (cookies.posales_auth === authCookie) return next();
  res.redirect("/login");
}

app.get("/login", (req, res) => {
  res.sendFile(path.join(__dirname, "public", "login.html"));
});

app.post("/login", (req, res) => {
  if (req.body.username === username && req.body.password === password) {
    res.setHeader("Set-Cookie", `posales_auth=${encodeURIComponent(authCookie)}; Path=/; HttpOnly; SameSite=Lax`);
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
  snapshot.receivedAt = new Date().toISOString();
  data.snapshots.unshift(snapshot);
  data.snapshots = data.snapshots.slice(0, 100);
  writeData(data);
  res.json({ ok: true, receivedAt: snapshot.receivedAt });
});

app.get("/api/reports", requireLogin, (req, res) => {
  const data = readData();
  res.json(data.snapshots[0] || {});
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
