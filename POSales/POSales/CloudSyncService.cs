using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace POSales
{
    class CloudSyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    class CloudSyncService
    {
        private readonly DBConnect dbcon = new DBConnect();

        public async Task<CloudSyncResult> UploadAsync(string username, IProgress<int> progress)
        {
            if (!dbcon.GetCloudSyncEnabled())
                return new CloudSyncResult { Success = false, Message = "Save to Cloud is disabled in Store Settings." };

            string url = dbcon.GetCloudUrl();
            if (string.IsNullOrWhiteSpace(url))
                return new CloudSyncResult { Success = false, Message = "Cloud URL is empty in Store Settings." };

            try
            {
                progress?.Report(10);
                string json = BuildSnapshotJson(username);
                progress?.Report(55);

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(45);
                    using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                    using (HttpResponseMessage response = await client.PostAsync(url, content))
                    {
                        progress?.Report(85);
                        if (!response.IsSuccessStatusCode)
                        {
                            return new CloudSyncResult
                            {
                                Success = false,
                                Message = "Cloud save failed: " + (int)response.StatusCode + " " + response.ReasonPhrase
                            };
                        }
                    }
                }

                progress?.Report(100);
                return new CloudSyncResult { Success = true, Message = "Reports saved to cloud successfully." };
            }
            catch (Exception ex)
            {
                return new CloudSyncResult
                {
                    Success = false,
                    Message = "Unable to save to cloud. Check your internet connection and Cloud URL. " + ex.Message
                };
            }
        }

        private string BuildSnapshotJson(string username)
        {
            bool includeUnsettled = dbcon.GetCloudIncludeUnsettled();
            StringBuilder json = new StringBuilder();
            json.Append("{");
            AppendProperty(json, "generatedAt", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
            json.Append(",");
            AppendProperty(json, "uploadedBy", username ?? "");
            json.Append(",");
            json.Append("\"store\":");
            json.Append(TableToObjectJson(Query("SELECT TOP 1 store, address, vat_type, vat_percent FROM tbStore")));
            json.Append(",");
            json.Append("\"sales\":");
            json.Append(TableToArrayJson(Query("SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total, c.sdate, c.cashier, ISNULL(c.paymenttype, 'Cash') AS paymenttype FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode = c.pcode WHERE c.status = 'Sold' ORDER BY c.sdate DESC, c.id DESC")));
            json.Append(",");
            json.Append("\"soldItems\":");
            json.Append(TableToArrayJson(Query("SELECT c.pcode, p.pdesc, c.price, SUM(c.qty) AS qty, SUM(c.disc) AS disc, SUM(c.total) AS total FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode = c.pcode WHERE c.status = 'Sold' GROUP BY c.pcode, p.pdesc, c.price ORDER BY total DESC")));
            json.Append(",");
            json.Append("\"topSelling\":");
            json.Append(TableToArrayJson(Query("SELECT TOP 10 c.pcode, p.pdesc, SUM(c.qty) AS qty, SUM(c.total) AS total FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode = c.pcode WHERE c.status = 'Sold' GROUP BY c.pcode, p.pdesc ORDER BY qty DESC")));
            json.Append(",");
            json.Append("\"inventory\":");
            json.Append(TableToArrayJson(QuerySafe("SELECT * FROM vwInventoryList")));
            json.Append(",");
            json.Append("\"criticalItems\":");
            json.Append(TableToArrayJson(QuerySafe("SELECT * FROM vwCriticalItems")));
            json.Append(",");
            json.Append("\"cancelled\":");
            json.Append(TableToArrayJson(QuerySafe("SELECT * FROM vwCancelItems")));
            json.Append(",");
            json.Append("\"unsettled\":");
            json.Append(includeUnsettled
                ? TableToArrayJson(Query("SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total, c.sdate, c.cashier FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode = c.pcode WHERE c.status = 'Pending' ORDER BY c.sdate DESC, c.id DESC"))
                : "[]");
            json.Append("}");
            return json.ToString();
        }

        private DataTable Query(string sql)
        {
            using (SqlConnection connection = new SqlConnection(dbcon.myConnection()))
            using (SqlCommand command = new SqlCommand(sql, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        private DataTable QuerySafe(string sql)
        {
            try
            {
                return Query(sql);
            }
            catch
            {
                return new DataTable();
            }
        }

        private static string TableToObjectJson(DataTable table)
        {
            if (table.Rows.Count == 0)
                return "{}";

            return RowToJson(table.Rows[0], table.Columns);
        }

        private static string TableToArrayJson(DataTable table)
        {
            StringBuilder json = new StringBuilder();
            json.Append("[");
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (i > 0)
                    json.Append(",");
                json.Append(RowToJson(table.Rows[i], table.Columns));
            }
            json.Append("]");
            return json.ToString();
        }

        private static string RowToJson(DataRow row, DataColumnCollection columns)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{");
            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    json.Append(",");
                AppendProperty(json, columns[i].ColumnName, row[columns[i]] == DBNull.Value ? "" : Convert.ToString(row[columns[i]], CultureInfo.InvariantCulture));
            }
            json.Append("}");
            return json.ToString();
        }

        private static void AppendProperty(StringBuilder json, string name, string value)
        {
            json.Append("\"");
            json.Append(JsonEscape(name));
            json.Append("\":\"");
            json.Append(JsonEscape(value));
            json.Append("\"");
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
