using AvinyaAICRM.Application.AI.Models;
using System.Collections.Generic;
using System.Linq;

namespace AvinyaAICRM.Application.AI.Pipeline
{
    public class LocalIntentClassifier
    {
        private readonly IntentStore _trainedStore;

        public LocalIntentClassifier(IntentStore trainedStore)
        {
            _trainedStore = trainedStore;
        }

        public ClassificationResult Classify(string message)
        {
            var lower = message.ToLower().Trim();

            // 0. Check Trained Knowledge (Learned from user text)
            var learnedIntent = _trainedStore.GetIntent(lower);
            if (learnedIntent != null)
                return Match(learnedIntent, 0.99);

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

            if (ContainsAny(lower, "project", "design"))
                return Match("query_projects", 0.90);

            if (ContainsAny(lower, "birthday", "anniversary", "born"))
                return Match("query_birthdays", 0.90);

            if (ContainsAny(lower, "stock", "inventory", "low", "out of"))
                return Match("query_low_stock", 0.90);

            if (ContainsAny(lower, "top", "vip", "biggest", "high value"))
                return Match("query_high_value_clients", 0.90);

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

            if (ContainsAny(lower, "report", "summary", "overview", "dashboard", "overall", "business", "doing"))
                return Match("report_summary", 0.90);

            return Match("unknown", 0.0);
        }

        public FilterResult ExtractFilters(string message)
        {
            var lower = message.ToLower();
            var filters = new FilterResult();

            // 1. Time period (Keyword based)
            if (lower.Contains("today"))
                filters.TimePeriod = "today";
            else if (lower.Contains("yesterday"))
                filters.TimePeriod = "yesterday";
            else if (ContainsAny(lower, "this week", "last 7 days", "weekly"))
                filters.TimePeriod = "this_week";
            else if (ContainsAny(lower, "this month", "current month", "monthly"))
                filters.TimePeriod = "this_month";
            else if (ContainsAny(lower, "last month", "previous month"))
                filters.TimePeriod = "last_month";
            else if (ContainsAny(lower, "this year", "current year", "yearly"))
                filters.TimePeriod = "this_year";

            // 1c. Specific Date Regex (e.g. "5 feb" or "10 march")
            var monthMatch = System.Text.RegularExpressions.Regex.Match(lower, @"(\d+)\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)");
            if (monthMatch.Success)
            {
                try
                {
                    int day = int.Parse(monthMatch.Groups[1].Value);
                    string m = monthMatch.Groups[2].Value.ToLower();
                    int month = m switch { "jan"=>1,"feb"=>2,"mar"=>3,"apr"=>4,"may"=>5,"jun"=>6,"jul"=>7,"aug"=>8,"sep"=>9,"oct"=>10,"nov"=>11,"dec"=>12, _=>0 };
                    if (month > 0)
                    {
                        filters.ExplicitDate = new System.DateTime(System.DateTime.Now.Year, month, day);
                    }
                } catch { }
            }

            // 1b. Advanced Regex for "last X days/weeks/months"
            var daysMatch = System.Text.RegularExpressions.Regex.Match(lower, @"last (\d+) day");
            if (daysMatch.Success)
            {
                filters.TimePeriod = $"last_{daysMatch.Groups[1].Value}_days";
            }

            // 2. Status filters (Advanced Keyword Mapping from DB Schema)
            var statusMap = new Dictionary<string, string>
            {
                { "pending", "Pending" },
                { "in progress", "In Progress" },
                { "approved", "Approved" },
                { "rejected", "Rejected" },
                { "accepted", "Accepted" },
                { "partial", "Partial" },
                { "receive", "Receive" },
                { "completed", "Completed" },
                { "done", "Done" },
                { "new", "New" },
                { "lost", "Lost" },
                { "sent", "Sent" },
                { "converted", "Converted" },
                { "jobwork", "JobWork" },
                { "inward", "Inward" },
                { "ready", "Ready" },
                { "dispatched", "Dispatched" },
                { "planning", "Planning" },
                { "active", "Active" },
                { "ready to dispatch", "Ready" }
            };

            foreach (var st in statusMap)
            {
                if (lower.Contains(st.Key))
                {
                    filters.ExplicitStatus = st.Value;
                    break;
                }
            }

            // 2b. Source filters (LeadSourceMaster)
            var sourceMap = new Dictionary<string, string>
            {
                { "walk-in", "Walk-in" },
                { "walkin", "Walk-in" },
                { "call", "Call" },
                { "referral", "Referral" },
                { "whatsapp", "WhatsApp" }
            };

            foreach (var src in sourceMap)
            {
                if (lower.Contains(src.Key))
                {
                    filters.ExplicitSource = src.Value;
                    break;
                }
            }

            // 3. Count vs List
            filters.IsCountQuery = ContainsAny(lower, "how many", "count", "total number", "number of");
            filters.IsSumQuery = ContainsAny(lower, "how much", "total amount", "sum", "revenue", "earned");

            // 4. My vs All
            filters.IsPersonalQuery = ContainsAny(lower, "my ", "mine", "assigned to me", "i have");

            // 5. Search Term extraction (for [name], of [name])
            var searchPatterns = new[] { @"for\s+([a-zA-Z0-9\-\/]+)", @"of\s+([a-zA-Z0-9\-\/]+)", @"named\s+([a-zA-Z0-9\-\/ ]+)", @"search\s+([a-zA-Z0-9\-\/ ]+)" };
            foreach (var pattern in searchPatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(lower, pattern);
                if (match.Success)
                {
                    filters.SearchTerm = match.Groups[1].Value.Trim();
                    break;
                }
            }
            
            // 6. Limit extraction (top 5, last 10, give 5, show 3)
            var limitMatch = System.Text.RegularExpressions.Regex.Match(lower, @"(?:top|last|give|show|first|only)\s+(\d+)");
            if (limitMatch.Success)
            {
                if (int.TryParse(limitMatch.Groups[1].Value, out int limit))
                {
                    filters.Limit = limit;
                }
            }

            return filters;
        }

        private bool ContainsAny(string text, params string[] keywords)
            => keywords.Any(k => text.Contains(k));

        private ClassificationResult Match(string intent, double confidence)
            => new() { Intent = intent, Confidence = confidence };
    }
}
