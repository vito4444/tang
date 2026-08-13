using System;
using Lingyan.Core.Officials;

namespace Lingyan.Core.Economy
{
    /// <summary>
    /// 月俸包：唐官俸三部分——月俸钱、禄米（年额按月折发）、职田（以租入折钱）。
    /// 数值为游戏化简表，量级参照《唐会要》禄秩门；正/从品同档，从品九折。
    /// 精确校数排在阶段 6 内容深化（docs/DECISIONS.md D17）。
    /// </summary>
    public sealed class MonthlyPay
    {
        /// <summary>月俸钱（文）。</summary>
        public long SalaryWen { get; set; }

        /// <summary>当月禄米（石，年额/12，向下取整到斗=0.1 石）。</summary>
        public double RiceShi { get; set; }

        /// <summary>职田租入折钱（文/月）。</summary>
        public long FieldRentWen { get; set; }

        /// <summary>米价折钱后的总额（文）。</summary>
        public long TotalWen { get; set; }
    }

    public static class SalaryTable
    {
        /// <summary>米价：文/石（游戏常数，阶段 6 随物价系统活化）。</summary>
        public const int RicePricePerShi = 200;

        /// <summary>年禄米（石）按主品。</summary>
        private static readonly int[] AnnualRiceByBand = { 0, 650, 470, 360, 260, 180, 120, 95, 72, 57 };

        /// <summary>月俸钱（文）按主品。</summary>
        private static readonly int[] MonthlyWenByBand = { 0, 31000, 24000, 17000, 11000, 8000, 5300, 3600, 2600, 1900 };

        /// <summary>职田（亩）按主品；租入取每亩每年 2 斗。</summary>
        private static readonly int[] FieldMuByBand = { 0, 1200, 1000, 900, 700, 500, 400, 350, 250, 200 };

        public static MonthlyPay For(RankGrade grade)
        {
            int band = grade.Band;
            double congFactor = grade.IsCong ? 0.9 : 1.0;

            long salary = (long)Math.Round(MonthlyWenByBand[band] * congFactor / 10.0) * 10;
            double riceYear = AnnualRiceByBand[band] * congFactor;
            double riceMonth = Math.Floor(riceYear / 12.0 * 10.0) / 10.0;
            double fieldRentYearShi = FieldMuByBand[band] * congFactor * 0.2; // 石
            long fieldRentMonth = (long)Math.Round(fieldRentYearShi * RicePricePerShi / 12.0 / 10.0) * 10;

            var pay = new MonthlyPay
            {
                SalaryWen = salary,
                RiceShi = riceMonth,
                FieldRentWen = fieldRentMonth
            };
            pay.TotalWen = pay.SalaryWen
                + (long)Math.Round(pay.RiceShi * RicePricePerShi)
                + pay.FieldRentWen;
            return pay;
        }
    }
}
