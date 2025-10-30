namespace Dict.DTO
{
    public class ZaloPayQueryResponse
    {
        public int return_code { get; set; }
        public string return_message { get; set; }
        public string app_trans_id { get; set; }
        public long amount { get; set; }
        public long zp_trans_id { get; set; } // 🔥 đổi từ string → long
        public string status { get; set; } // "1" = success, "0" = pending, "-1" = failed
        public long server_time { get; set; }
    }
}
