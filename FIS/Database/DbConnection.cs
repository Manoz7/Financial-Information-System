using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace FIS.Database
{
    public static class DBHelper
    {
        // Reads connection string from App.config
        // Key must be "FIS_DB" in your App.config connectionStrings section
        private static string ConnStr =>
            ConfigurationManager.ConnectionStrings["FIS_DB"].ConnectionString;

        // ── SELECT queries — returns a DataTable ─────────────────────────────
        public static DataTable ExecuteQuery(string sql,
            params MySqlParameter[] parms)
        {
            var dt = new DataTable();
            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parms != null) cmd.Parameters.AddRange(parms);
                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }
            return dt;
        }

        // ── INSERT / UPDATE / DELETE — returns rows affected ─────────────────
        public static int ExecuteNonQuery(string sql,
            params MySqlParameter[] parms)
        {
            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parms != null) cmd.Parameters.AddRange(parms);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Returns single value — use for COUNT, SUM, last insert ID, etc. ──
        public static object ExecuteScalar(string sql,
            params MySqlParameter[] parms)
        {
            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parms != null) cmd.Parameters.AddRange(parms);
                    return cmd.ExecuteScalar();
                }
            }
        }

        // ── Ping the database — use on app startup to verify connectivity ────
        public static bool TestConnection()
        {
            try
            {
                using (var conn = new MySqlConnection(ConnStr))
                    conn.Open();
                return true;
            }
            catch { return false; }
        }

        // ── Get the last auto-incremented ID after an INSERT ─────────────────
        // Usage: int newId = DBHelper.GetLastInsertId();
        public static int GetLastInsertId()
        {
            return Convert.ToInt32(ExecuteScalar("SELECT LAST_INSERT_ID();"));
        }
    }
}