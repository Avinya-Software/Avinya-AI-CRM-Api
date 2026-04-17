using AvinyaAICRM.Application.AI.Models;
using System.Collections.Generic;
using System.Linq;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class LocalIntentClassifier
    {
        public ClassificationResult Classify(string message)
        {
            var lower = message.ToLower().Trim();

            // --- CREATE INTENTS ---
            if (ContainsAny(lower, "create lead", "add lead", "new lead", "add a lead", "capture lead"))
                return Match("create_lead", 0.99);

            if (ContainsAny(lower, "create task", "add task", "new task", "remind me", "set reminder", "todo"))
                return Match("create_task", 0.99);

            // --- QUERY INTENTS ---
            if (ContainsAny(lower, "follow up", "followup", "follow-up", "next followup"))
                return Match("query_followups", 0.95);

            if (ContainsAny(lower, "revenue", "earned", "income", "sales amount", "total sales", "profit"))
                return Match("query_revenue", 0.95);

            if (ContainsAny(lower, "lead", "leads", "enquiry", "enquiries"))
                return Match("query_leads", 0.90);

            if (ContainsAny(lower, "order", "orders", "booking", "bookings"))
                return Match("query_orders", 0.90);

            if (ContainsAny(lower, "quotation", "quote", "quotations", "quotes", "proposal"))
                return Match("query_quotations", 0.90);

            if (ContainsAny(lower, "task", "tasks", "todo", "to-do", "to do"))
                return Match("query_tasks", 0.90);

            if (ContainsAny(lower, "client", "clients", "customer", "customers"))
                return Match("query_clients", 0.90);

            if (ContainsAny(lower, "expense", "expenses", "spend", "spending", "cost"))
                return Match("query_expenses", 0.90);

            if (ContainsAny(lower, "project", "projects"))
                return Match("query_projects", 0.90);

            if (ContainsAny(lower, "invoice", "invoices", "bill", "bills"))
                return Match("query_invoices", 0.95);

            if (ContainsAny(lower, "payment", "payments", "received", "collection"))
                return Match("query_payments", 0.95);

            if (ContainsAny(lower, "product", "products", "item", "items", "inventory"))
                return Match("query_products", 0.95);

            if (lower.Contains("source") && (lower.Contains("lead") || lower.Contains("enquiry")))
                return Match("query_leads_source", 0.95);

            if (ContainsAny(lower, "staff", "team", "performance", "top employee", "who created"))
                return Match("query_staff_performance", 0.95);

            if (ContainsAny(lower, "tax", "gst", "cgst", "sgst", "igst"))
                return Match("query_tax_summary", 0.95);

            if (ContainsAny(lower, "trend", "monthly breakdown", "growth", "over time"))
                return Match("query_revenue_trend", 0.95);

            if (ContainsAny(lower, "top client", "best client", "highest business", "valuable customer"))
                return Match("query_top_clients", 0.95);

            if (ContainsAny(lower, "expiring", "valid till", "about to expire"))
                return Match("query_expiring_quotations", 0.95);

            if (ContainsAny(lower, "design", "graphic", "artwork"))
                return Match("query_design_orders", 0.95);

            if (ContainsAny(lower, "overdue", "late", "past due"))
                return Match("query_overdue_items", 0.95);

            if (ContainsAny(lower, "inactive", "quiet", "no order", "no booking"))
                return Match("query_inactive_clients", 0.95);

            if (ContainsAny(lower, "recent", "latest", "what happened", "activity", "newly added"))
                return Match("query_recent_activity", 0.95);

            if (ContainsAny(lower, "report", "summary", "overview", "dashboard", "overall"))
                return Match("report_summary", 0.90);

            return Match("unknown", 0.0);
        }

        public FilterResult ExtractFilters(string message)
        {
            var lower = message.ToLower();
            var filters = new FilterResult();

            // Time period
            if (lower.Contains("today"))
                filters.TimePeriod = "today";
            else if (ContainsAny(lower, "this week", "last 7 days"))
                filters.TimePeriod = "this_week";
            else if (ContainsAny(lower, "this month", "current month"))
                filters.TimePeriod = "this_month";
            else if (ContainsAny(lower, "last month", "previous month"))
                filters.TimePeriod = "last_month";
            else if (ContainsAny(lower, "this year", "current year"))
                filters.TimePeriod = "this_year";
            else if (lower.Contains("yesterday"))
                filters.TimePeriod = "yesterday";

            // Status filters
            if (ContainsAny(lower, "pending", "open", "active"))
                filters.Status = "Pending";
            else if (ContainsAny(lower, "completed", "done", "closed"))
                filters.Status = "Completed";
            else if (ContainsAny(lower, "converted", "won"))
                filters.Status = "Converted";
            else if (ContainsAny(lower, "lost", "rejected", "failed"))
                filters.Status = "Lost";

            // Count vs List
            filters.IsCountQuery = ContainsAny(lower, "how many", "count", "total number", "number of");
            filters.IsSumQuery = ContainsAny(lower, "how much", "total amount", "sum", "revenue", "earned");

            // My vs All
            filters.IsPersonalQuery = ContainsAny(lower, "my ", "mine", "assigned to me", "i have");

            return filters;
        }

        private bool ContainsAny(string text, params string[] keywords)
            => keywords.Any(k => text.Contains(k));

        private ClassificationResult Match(string intent, double confidence)
            => new() { Intent = intent, Confidence = confidence };
    }
}
