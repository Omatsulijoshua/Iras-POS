const money = new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
let latestReport = {};

function number(value) {
  const parsed = Number(String(value || "0").replace(/,/g, ""));
  return Number.isFinite(parsed) ? parsed : 0;
}

function total(rows, field) {
  return (rows || []).reduce((sum, row) => sum + number(row[field]), 0);
}

function renderTable(id, rows, columns) {
  const table = document.getElementById(id);
  const body = (rows || []).slice(0, 100).map((row) => {
    return `<tr>${columns.map((column) => `<td>${escapeHtml(row[column] ?? "")}</td>`).join("")}</tr>`;
  }).join("");
  table.innerHTML = `<thead><tr>${columns.map((column) => `<th>${column}</th>`).join("")}</tr></thead><tbody>${body || `<tr><td colspan="${columns.length}">No data uploaded yet</td></tr>`}</tbody>`;
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, (char) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    "\"": "&quot;",
    "'": "&#039;"
  })[char]);
}

function rowDateValue(row) {
  const raw = row.sdate || row.date || "";
  const parsed = new Date(raw);
  if (!Number.isNaN(parsed.getTime())) return parsed;

  const match = String(raw).match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})/);
  if (match) return new Date(Number(match[3]), Number(match[1]) - 1, Number(match[2]));
  return null;
}

function dateInputValue(date) {
  if (!date) return "";
  const local = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  return local.toISOString().slice(0, 10);
}

function normalizeDay(value, endOfDay = false) {
  if (!value) return null;
  const date = new Date(`${value}T00:00:00`);
  if (endOfDay) date.setHours(23, 59, 59, 999);
  return date;
}

function currentFilters() {
  return {
    system: document.getElementById("systemFilter").value,
    cashier: document.getElementById("cashierFilter").value,
    dateFrom: normalizeDay(document.getElementById("dateFromFilter").value),
    dateTo: normalizeDay(document.getElementById("dateToFilter").value, true)
  };
}

function filterRows(rows) {
  const filters = currentFilters();
  return (rows || []).filter((row) => {
    if (filters.system !== "All Systems" && String(row.systemName || "") !== filters.system) return false;
    if (filters.cashier !== "All Cashier" && String(row.cashier || "") !== filters.cashier) return false;
    const date = rowDateValue(row);
    if (filters.dateFrom && date && date < filters.dateFrom) return false;
    if (filters.dateTo && date && date > filters.dateTo) return false;
    if ((filters.dateFrom || filters.dateTo) && !date) return false;
    return true;
  });
}

function uniqueSystems(data) {
  const names = new Set();
  (data.systems || []).forEach((system) => {
    const systemName = String(system.systemName || "").trim();
    if (systemName) names.add(systemName);
  });
  [...(data.sales || []), ...(data.unsettled || []), ...(data.inventory || [])].forEach((row) => {
    const systemName = String(row.systemName || "").trim();
    if (systemName) names.add(systemName);
  });
  return Array.from(names).sort((a, b) => a.localeCompare(b));
}

function uniqueCashiers(data) {
  const names = new Set();
  [...(data.sales || []), ...(data.unsettled || [])].forEach((row) => {
    const cashier = String(row.cashier || "").trim();
    if (cashier) names.add(cashier);
  });
  return Array.from(names).sort((a, b) => a.localeCompare(b));
}

function populateSystems(data) {
  const select = document.getElementById("systemFilter");
  const selected = select.value || "All Systems";
  const options = ["All Systems", ...uniqueSystems(data)];
  select.innerHTML = options.map((name) => `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`).join("");
  select.value = options.includes(selected) ? selected : "All Systems";
}

function populateCashiers(data) {
  const select = document.getElementById("cashierFilter");
  const selected = select.value || "All Cashier";
  const options = ["All Cashier", ...uniqueCashiers(data)];
  select.innerHTML = options.map((name) => `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`).join("");
  select.value = options.includes(selected) ? selected : "All Cashier";
}

function defaultDateRange(data) {
  const fromInput = document.getElementById("dateFromFilter");
  const toInput = document.getElementById("dateToFilter");
  if (fromInput.value || toInput.value) return;
  const today = new Date();
  fromInput.value = dateInputValue(today);
  toInput.value = dateInputValue(today);
}

function topSellingFromRows(rows) {
  const grouped = new Map();
  rows.forEach((row) => {
    const key = `${row.pcode || ""}|${row.pdesc || ""}`;
    const existing = grouped.get(key) || { pcode: row.pcode || "", pdesc: row.pdesc || "", qty: 0, total: 0 };
    existing.qty += number(row.qty);
    existing.total += number(row.total);
    grouped.set(key, existing);
  });
  return Array.from(grouped.values())
    .sort((a, b) => b.qty - a.qty)
    .slice(0, 10)
    .map((row) => ({ ...row, qty: String(row.qty), total: money.format(row.total) }));
}

function renderDashboard(data) {
  const sales = filterRows(data.sales || []);
  const unsettled = filterRows(data.unsettled || []);
  const inventory = filterRowsWithoutDates(data.inventory || []);
  const store = data.store || {};
  const todayRows = (data.sales || []).filter((row) => {
    const date = rowDateValue(row);
    if (!date) return false;
    const today = new Date();
    return date.getFullYear() === today.getFullYear() && date.getMonth() === today.getMonth() && date.getDate() === today.getDate();
  });

  const globalSystemsCount = data.systems ? data.systems.length : 0;
  const globalProductCount = data.systems ? data.systems.reduce((sum, sys) => sum + number(sys.productCount), 0) : 0;
  const globalTodaySales = total(todayRows, "total");

  document.getElementById("globalSystemsCount").textContent = globalSystemsCount;
  document.getElementById("globalProductCount").textContent = globalProductCount;
  document.getElementById("globalTodaySales").textContent = money.format(globalTodaySales);

  document.getElementById("storeName").textContent = store.store || "POSales Cloud Reports";
  document.getElementById("syncMeta").textContent = data.receivedAt ? `Last sync: ${new Date(data.receivedAt).toLocaleString()} by ${data.uploadedBy || "POS"}` : "Waiting for uploaded reports";
  document.getElementById("todaySalesTotal").textContent = money.format(total(todayRows, "total"));
  document.getElementById("salesTotal").textContent = money.format(total(sales, "total"));
  document.getElementById("transactions").textContent = new Set(sales.map((row) => row.transno)).size;
  document.getElementById("itemsSold").textContent = sales.reduce((sum, row) => sum + number(row.qty), 0);
  document.getElementById("productCount").textContent = inventory.length;
  document.getElementById("unsettledTotal").textContent = money.format(total(unsettled, "total"));

  renderTable("systemsTable", data.systems || [], ["systemName", "storeName", "productCount", "salesCount", "receivedAt"]);
  renderTable("salesTable", sales, ["systemName", "transno", "sdate", "cashier", "pcode", "pdesc", "price", "qty", "disc", "total", "paymenttype"]);
  renderTable("unsettledTable", unsettled, ["systemName", "transno", "sdate", "cashier", "pcode", "pdesc", "price", "qty", "disc", "total"]);
  renderTable("topTable", topSellingFromRows(sales), ["pcode", "pdesc", "qty", "total"]);
  const criticalRows = filterRowsWithoutDates(data.criticalItems || []);
  renderTable("criticalTable", criticalRows, Object.keys(criticalRows[0] || { systemName: "", pcode: "", pdesc: "", qty: "", reorder: "" }));

  const stockIn = filterRowsWithoutDates(data.stockIn || []);
  const formattedStockIn = stockIn.map(row => ({
    ...row,
    Price: money.format(number(row.Price)),
    Amount: money.format(number(row.Amount)),
    RecordDate: row.RecordDate ? new Date(row.RecordDate).toLocaleString() : ""
  }));
  renderTable("stockInTable", formattedStockIn, ["systemName", "ReferenceNo", "RecordDate", "RecordedBy", "Pcode", "Description", "Price", "Qty", "Amount", "Supplier"]);
}

function filterRowsWithoutDates(rows) {
  const filters = currentFilters();
  return (rows || []).filter((row) => {
    if (filters.system !== "All Systems" && String(row.systemName || "") !== filters.system) return false;
    return true;
  });
}

async function loadReports() {
  const response = await fetch("/api/reports");
  if (response.status === 401) {
    window.location.href = "/login";
    return;
  }
  latestReport = await response.json();
  populateSystems(latestReport);
  populateCashiers(latestReport);
  defaultDateRange(latestReport);
  renderDashboard(latestReport);
}

document.getElementById("systemFilter").addEventListener("change", () => renderDashboard(latestReport));
document.getElementById("cashierFilter").addEventListener("change", () => renderDashboard(latestReport));
document.getElementById("dateFromFilter").addEventListener("change", () => renderDashboard(latestReport));
document.getElementById("dateToFilter").addEventListener("change", () => renderDashboard(latestReport));
document.getElementById("clearFilters").addEventListener("click", () => {
  document.getElementById("systemFilter").value = "All Systems";
  document.getElementById("cashierFilter").value = "All Cashier";
  const today = dateInputValue(new Date());
  document.getElementById("dateFromFilter").value = today;
  document.getElementById("dateToFilter").value = today;
  renderDashboard(latestReport);
});

async function loadAdminUsers() {
  const response = await fetch("/api/admin/users");
  if (response.status === 401) {
    window.location.href = "/login";
    return;
  }
  if (!response.ok) return;
  const data = await response.json();
  renderTable("adminUsersTable", data.users || [], ["username"]);
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  if (response.status === 401) {
    window.location.href = "/login";
    return;
  }
  const data = await response.json().catch(() => ({}));
  if (!response.ok || data.ok === false) throw new Error(data.error || "Request failed.");
  return data;
}

document.getElementById("passwordForm").addEventListener("submit", async (event) => {
  event.preventDefault();
  const message = document.getElementById("adminMessage");
  try {
    await postJson("/api/admin/password", {
      currentPassword: document.getElementById("currentPassword").value,
      newPassword: document.getElementById("newPassword").value
    });
    document.getElementById("passwordForm").reset();
    message.textContent = "Password changed.";
  } catch (error) {
    message.textContent = error.message;
  }
});

document.getElementById("adminForm").addEventListener("submit", async (event) => {
  event.preventDefault();
  const message = document.getElementById("adminMessage");
  try {
    await postJson("/api/admin/users", {
      username: document.getElementById("adminUsername").value,
      password: document.getElementById("adminPassword").value
    });
    document.getElementById("adminForm").reset();
    message.textContent = "Admin created.";
    await loadAdminUsers();
  } catch (error) {
    message.textContent = error.message;
  }
});

// Initialize date filters to today immediately
const todayDateStr = dateInputValue(new Date());
document.getElementById("dateFromFilter").value = todayDateStr;
document.getElementById("dateToFilter").value = todayDateStr;

loadReports();
loadAdminUsers();
setInterval(loadReports, 60000);

// Inventory Modal Interactivity
const modal = document.getElementById("inventoryModal");
const closeBtn = document.querySelector(".close-modal");
const searchInput = document.getElementById("inventorySearch");

function openInventoryModal() {
  modal.style.display = "block";
  searchInput.value = "";
  renderInventoryTable();
  searchInput.focus();
}

document.getElementById("globalProductCard").addEventListener("click", openInventoryModal);
document.getElementById("productMetricCard").addEventListener("click", openInventoryModal);

closeBtn.addEventListener("click", () => {
  modal.style.display = "none";
});

window.addEventListener("click", (event) => {
  if (event.target === modal) {
    modal.style.display = "none";
  }
});

searchInput.addEventListener("input", () => {
  renderInventoryTable();
});

function renderInventoryTable() {
  const searchTerm = searchInput.value.toLowerCase();
  const inventory = filterRowsWithoutDates(latestReport.inventory || []);
  
  const filtered = inventory.filter(row => {
    return (
      String(row.pcode || "").toLowerCase().includes(searchTerm) ||
      String(row.pdesc || "").toLowerCase().includes(searchTerm) ||
      String(row.brand || "").toLowerCase().includes(searchTerm) ||
      String(row.category || "").toLowerCase().includes(searchTerm)
    );
  });
  
  const formatted = filtered.map(row => ({
    ...row,
    price: money.format(number(row.price))
  }));
  
  renderTable("inventoryTable", formatted, ["systemName", "pcode", "pdesc", "brand", "category", "price", "qty", "reorder"]);
}
