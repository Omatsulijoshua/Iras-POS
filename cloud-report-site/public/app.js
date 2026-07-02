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
    return `<tr>${columns.map((column) => `<td>${row[column] ?? ""}</td>`).join("")}</tr>`;
  }).join("");
  table.innerHTML = `<thead><tr>${columns.map((column) => `<th>${column}</th>`).join("")}</tr></thead><tbody>${body || `<tr><td colspan="${columns.length}">No data uploaded yet</td></tr>`}</tbody>`;
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
    cashier: document.getElementById("cashierFilter").value,
    dateFrom: normalizeDay(document.getElementById("dateFromFilter").value),
    dateTo: normalizeDay(document.getElementById("dateToFilter").value, true)
  };
}

function filterRows(rows) {
  const filters = currentFilters();
  return (rows || []).filter((row) => {
    if (filters.cashier !== "All Cashier" && String(row.cashier || "") !== filters.cashier) return false;
    const date = rowDateValue(row);
    if (filters.dateFrom && date && date < filters.dateFrom) return false;
    if (filters.dateTo && date && date > filters.dateTo) return false;
    if ((filters.dateFrom || filters.dateTo) && !date) return false;
    return true;
  });
}

function uniqueCashiers(data) {
  const names = new Set();
  [...(data.sales || []), ...(data.unsettled || [])].forEach((row) => {
    const cashier = String(row.cashier || "").trim();
    if (cashier) names.add(cashier);
  });
  return Array.from(names).sort((a, b) => a.localeCompare(b));
}

function populateCashiers(data) {
  const select = document.getElementById("cashierFilter");
  const selected = select.value || "All Cashier";
  const options = ["All Cashier", ...uniqueCashiers(data)];
  select.innerHTML = options.map((name) => `<option value="${name}">${name}</option>`).join("");
  select.value = options.includes(selected) ? selected : "All Cashier";
}

function defaultDateRange(data) {
  const fromInput = document.getElementById("dateFromFilter");
  const toInput = document.getElementById("dateToFilter");
  if (fromInput.value || toInput.value) return;

  const dates = [...(data.sales || []), ...(data.unsettled || [])]
    .map(rowDateValue)
    .filter(Boolean)
    .sort((a, b) => a - b);

  if (dates.length === 0) return;
  fromInput.value = dateInputValue(dates[0]);
  toInput.value = dateInputValue(dates[dates.length - 1]);
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
  const store = data.store || {};

  document.getElementById("storeName").textContent = store.store || "POSales Cloud Reports";
  document.getElementById("syncMeta").textContent = data.receivedAt ? `Last sync: ${new Date(data.receivedAt).toLocaleString()} by ${data.uploadedBy || "POS"}` : "Waiting for uploaded reports";
  document.getElementById("salesTotal").textContent = money.format(total(sales, "total"));
  document.getElementById("transactions").textContent = new Set(sales.map((row) => row.transno)).size;
  document.getElementById("itemsSold").textContent = sales.reduce((sum, row) => sum + number(row.qty), 0);
  document.getElementById("unsettledTotal").textContent = money.format(total(unsettled, "total"));

  renderTable("salesTable", sales, ["transno", "sdate", "cashier", "pcode", "pdesc", "price", "qty", "disc", "total", "paymenttype"]);
  renderTable("unsettledTable", unsettled, ["transno", "sdate", "cashier", "pcode", "pdesc", "price", "qty", "disc", "total"]);
  renderTable("topTable", topSellingFromRows(sales), ["pcode", "pdesc", "qty", "total"]);
  renderTable("criticalTable", data.criticalItems || [], Object.keys((data.criticalItems || [])[0] || { pcode: "", pdesc: "", qty: "", reorder: "" }));
}

async function loadReports() {
  const response = await fetch("/api/reports");
  latestReport = await response.json();
  populateCashiers(latestReport);
  defaultDateRange(latestReport);
  renderDashboard(latestReport);
}

document.getElementById("cashierFilter").addEventListener("change", () => renderDashboard(latestReport));
document.getElementById("dateFromFilter").addEventListener("change", () => renderDashboard(latestReport));
document.getElementById("dateToFilter").addEventListener("change", () => renderDashboard(latestReport));
document.getElementById("clearFilters").addEventListener("click", () => {
  document.getElementById("cashierFilter").value = "All Cashier";
  document.getElementById("dateFromFilter").value = "";
  document.getElementById("dateToFilter").value = "";
  renderDashboard(latestReport);
});

loadReports();
setInterval(loadReports, 60000);
