using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OptiCore.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace OptiCore.Services
{
    public class ReportService
    {
        public string ExportTxt(HardwareProfile profile, SystemMetrics metrics, List<OptimizationItem> applied)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==============================================");
            sb.AppendLine("          OPTICORE — OPTIMIZATION REPORT     ");
            sb.AppendLine("==============================================");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("HARDWARE PROFILE");
            sb.AppendLine($"  CPU: {profile.CpuModel} ({profile.CpuPhysicalCores}C/{profile.CpuLogicalCores}T, {profile.CpuSocket})");
            sb.AppendLine($"  GPU: {profile.GpuModel}");
            sb.AppendLine($"  RAM: {profile.RamTotalGb}GB @ {profile.RamSpeedMts}MHz");
            sb.AppendLine($"  OS:  {profile.WindowsVersion} Build {profile.WindowsBuild}");
            sb.AppendLine();
            sb.AppendLine("METRICS");
            sb.AppendLine($"  {"Metric",-25} {"Before",-12} {"After",-12} {"Delta",-12} {"% Change"}");
            sb.AppendLine($"  {new string('-', 65)}");
            AppendMetricLine(sb, "IRQs/sec", metrics.IrqPerSecBefore, metrics.IrqPerSecAfter, metrics.BeforeMeasured, metrics.AfterMeasured);
            AppendMetricLine(sb, "DPCs/sec", metrics.DpcPerSecBefore, metrics.DpcPerSecAfter, metrics.BeforeMeasured, metrics.AfterMeasured);
            AppendMetricLine(sb, "Timer Res (ms)", metrics.TimerResolutionMsBefore, metrics.TimerResolutionMsAfter, metrics.BeforeMeasured, metrics.AfterMeasured);
            AppendMetricLine(sb, "Running Services", metrics.RunningServicesBefore, metrics.RunningServicesAfter, metrics.BeforeMeasured, metrics.AfterMeasured);
            AppendMetricLine(sb, "Active Tasks", metrics.ActiveTasksBefore, metrics.ActiveTasksAfter, metrics.BeforeMeasured, metrics.AfterMeasured);
            if (metrics.MsiModeActivated)
            {
                sb.AppendLine();
                sb.AppendLine("  NOTE (GPU MSI Mode): IRQs/sec may appear higher because 16 MSI vectors are now");
                sb.AppendLine("  reported separately instead of 1 shared IRQ line. This is expected and improves");
                sb.AppendLine("  GPU interrupt latency. The real benefit is lower DPC latency, not lower IRQ count.");
            }
            sb.AppendLine();
            sb.AppendLine("OPTIMIZATIONS APPLIED");
            foreach (var item in applied.Where(i => i.IsApplied))
            {
                var status = item.ApplyFailed ? "[FAILED]" : "[OK]";
                var reboot = item.RequiresReboot ? " *Reboot required" : "";
                sb.AppendLine($"  {status} {item.Name}{reboot}");
            }
            sb.AppendLine();
            sb.AppendLine("--- Brazilian Top Team | OptiCore v1.5.0 ---");
            return sb.ToString();
        }

        private void AppendMetricLine(StringBuilder sb, string name, double before, double after, bool hasBefore, bool hasAfter)
        {
            var beforeStr = hasBefore ? before.ToString("N1") : "N/A";
            var afterStr = hasAfter ? after.ToString("N1") : "N/A";
            var delta = (hasBefore && hasAfter) ? (after - before).ToString("N1") : "N/A";
            var pct = (hasBefore && hasAfter && before != 0) ? $"{((after - before) / before * 100):N1}%" : "N/A";
            sb.AppendLine($"  {name,-25} {beforeStr,-12} {afterStr,-12} {delta,-12} {pct}");
        }

        public string ExportPdf(HardwareProfile profile, SystemMetrics metrics, List<OptimizationItem> applied)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"OptiCore_Report_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf");

            try
            {
                var doc = new PdfDocument();
                doc.Info.Title = "OptiCore Report";
                doc.Info.Author = "Brazilian Top Team";

                var page = doc.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);

                var titleFont    = new XFont("Segoe UI", 20, XFontStyleEx.Bold);
                var headerFont   = new XFont("Segoe UI", 12, XFontStyleEx.Bold);
                var tableHdrFont = new XFont("Segoe UI", 8,  XFontStyleEx.Bold);
                var bodyFont     = new XFont("Segoe UI", 9,  XFontStyleEx.Regular);
                var noteFont     = new XFont("Segoe UI", 7.5, XFontStyleEx.Regular);
                var bg            = XColor.FromArgb(13,  17,  23);
                var accent        = XColor.FromArgb(192, 57,  43);
                var textPrimary   = XColor.FromArgb(230, 237, 243);
                var textSecondary = XColor.FromArgb(139, 148, 158);
                var green         = XColor.FromArgb(39,  174, 96);
                var red           = XColor.FromArgb(192, 57,  43);
                var orange        = XColor.FromArgb(230, 126, 34);

                double pageW = page.Width.Point;
                double pageH = page.Height.Point;
                gfx.DrawRectangle(new XSolidBrush(bg), 0, 0, pageW, pageH);

                double y = 40;

                // Title
                gfx.DrawString("OptiCore", titleFont, new XSolidBrush(accent), new XRect(0, y, pageW, 30), XStringFormats.TopCenter);
                y += 35;
                gfx.DrawString("Windows Performance Optimizer — Report", bodyFont, new XSolidBrush(textSecondary), new XRect(0, y, pageW, 20), XStringFormats.TopCenter);
                y += 25;
                gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", bodyFont, new XSolidBrush(textSecondary), new XRect(40, y, pageW - 80, 20), XStringFormats.TopLeft);
                y += 30;

                // Hardware Profile
                gfx.DrawString("Hardware Profile", headerFont, new XSolidBrush(textPrimary), 40, y);
                y += 20;
                gfx.DrawString($"CPU: {profile.CpuModel}", bodyFont, new XSolidBrush(textPrimary), 60, y); y += 15;
                gfx.DrawString($"GPU: {profile.GpuModel}", bodyFont, new XSolidBrush(textPrimary), 60, y); y += 15;
                gfx.DrawString($"RAM: {profile.RamTotalGb}GB @ {profile.RamSpeedMts}MHz", bodyFont, new XSolidBrush(textPrimary), 60, y); y += 25;

                // Performance Metrics table
                gfx.DrawString("Performance Metrics", headerFont, new XSolidBrush(textPrimary), 40, y);
                y += 18;

                double[] colX = { 40, 175, 250, 325, 400 };
                string[] colHeaders = { "Metric", "Before", "After", "Delta", "% Change" };
                for (int ci = 0; ci < colHeaders.Length; ci++)
                    gfx.DrawString(colHeaders[ci], tableHdrFont, new XSolidBrush(textSecondary), colX[ci], y);
                y += 4;
                gfx.DrawLine(new XPen(textSecondary, 0.5), 40, y, pageW - 40, y);
                y += 11;

                void DrawMetricRow(string name, double before, double after, bool hasBefore, bool hasAfter)
                {
                    var bStr = hasBefore ? before.ToString("N1") : "N/A";
                    var aStr = hasAfter  ? after.ToString("N1")  : "N/A";
                    var dStr = (hasBefore && hasAfter) ? (after - before).ToString("N1") : "N/A";
                    var pStr = (hasBefore && hasAfter && before != 0) ? $"{((after - before) / before * 100):N1}%" : "N/A";
                    XColor pColor = (hasBefore && hasAfter && before != 0)
                        ? (after <= before ? green : red)
                        : textSecondary;
                    gfx.DrawString(name, bodyFont, new XSolidBrush(textPrimary),   colX[0], y);
                    gfx.DrawString(bStr, bodyFont, new XSolidBrush(textSecondary), colX[1], y);
                    gfx.DrawString(aStr, bodyFont, new XSolidBrush(textPrimary),   colX[2], y);
                    gfx.DrawString(dStr, bodyFont, new XSolidBrush(textSecondary), colX[3], y);
                    gfx.DrawString(pStr, bodyFont, new XSolidBrush(pColor),        colX[4], y);
                    y += 13;
                }

                DrawMetricRow("IRQs/sec",         metrics.IrqPerSecBefore,         metrics.IrqPerSecAfter,         metrics.BeforeMeasured, metrics.AfterMeasured);
                DrawMetricRow("DPCs/sec",         metrics.DpcPerSecBefore,         metrics.DpcPerSecAfter,         metrics.BeforeMeasured, metrics.AfterMeasured);
                DrawMetricRow("Timer Res (ms)",   metrics.TimerResolutionMsBefore, metrics.TimerResolutionMsAfter, metrics.BeforeMeasured, metrics.AfterMeasured);
                DrawMetricRow("Running Services", metrics.RunningServicesBefore,   metrics.RunningServicesAfter,   metrics.BeforeMeasured, metrics.AfterMeasured);
                DrawMetricRow("Active Tasks",     metrics.ActiveTasksBefore,       metrics.ActiveTasksAfter,       metrics.BeforeMeasured, metrics.AfterMeasured);

                if (metrics.MsiModeActivated)
                {
                    y += 4;
                    const double noteH = 28;
                    gfx.DrawRectangle(new XPen(orange, 1), 40, y, pageW - 80, noteH);
                    gfx.DrawString(
                        "NOTE — GPU MSI Mode: Interrupts distributed across 16 vectors. IRQs/sec may appear higher — this is expected and improves latency.",
                        noteFont, new XSolidBrush(orange),
                        new XRect(46, y + 5, pageW - 92, noteH - 5), XStringFormats.TopLeft);
                    y += noteH + 8;
                }

                y += 10;

                // Optimizations Applied
                gfx.DrawString("Optimizations Applied", headerFont, new XSolidBrush(textPrimary), 40, y);
                y += 20;
                foreach (var item in applied.Where(i => i.IsApplied))
                {
                    var color = item.ApplyFailed ? XColor.FromArgb(231, 76, 60) : XColor.FromArgb(39, 174, 96);
                    gfx.DrawString($"✓ {item.Name}", bodyFont, new XSolidBrush(color), 60, y);
                    y += 14;
                    if (y > pageH - 60) break;
                }

                doc.Save(path);
                return path;
            }
            catch (Exception ex)
            {
                return $"PDF export failed: {ex.Message}";
            }
        }

        public string GetClipboardSummary(HardwareProfile profile, SystemMetrics metrics, List<OptimizationItem> applied)
        {
            var count = applied.Count(i => i.IsApplied);
            return $"OptiCore v1.4.0 — {count} optimizations applied on {profile.CpuModel} + {profile.GpuModel} | {DateTime.Now:yyyy-MM-dd} | Brazilian Top Team";
        }
    }
}
