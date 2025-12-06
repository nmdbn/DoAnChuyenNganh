using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DoAnChuyenNganh.Services
{
    public class VnPayLibrary
    {
        public const string VERSION = "2.1.0";

        // luôn sort theo key tăng dần
        private readonly SortedList<string, string> _requestData =
            new SortedList<string, string>(new VnPayCompare());

        private readonly SortedList<string, string> _responseData =
            new SortedList<string, string>(new VnPayCompare());

        // -------------------------------------------------------
        // REQUEST
        // -------------------------------------------------------
        public void AddRequestData(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return;

            // VNPay chỉ dùng các param bắt đầu bằng vnp_
            if (!key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                return;

            _requestData[key] = value;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            var sb = new StringBuilder();

            foreach (var kv in _requestData)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    continue;

                sb.Append(VnPayUrlEncode(kv.Key));
                sb.Append('=');
                sb.Append(VnPayUrlEncode(kv.Value));
                sb.Append('&');
            }

            if (sb.Length > 0)
                sb.Length -= 1; // bỏ dấu & cuối

            string queryString = sb.ToString();

            // Chuỗi dùng để ký
            string signData = queryString;

            Console.WriteLine("========== VNPAY SIGN DATA ==========");
            Console.WriteLine(signData);
            Console.WriteLine("=====================================");

            // Ký HMACSHA512
            string vnp_SecureHash = Utils.HmacSHA512(vnp_HashSecret, signData);

            // Gắn hash vào URL
            var url = new StringBuilder();
            url.Append(baseUrl);
            url.Append('?');
            url.Append(queryString);
            url.Append("&vnp_SecureHash=");
            url.Append(vnp_SecureHash);

            return url.ToString();
        }

        // -------------------------------------------------------
        // RESPONSE
        // -------------------------------------------------------
        public void AddResponseData(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return;

            if (!key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                return;

            _responseData[key] = value;
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            if (string.IsNullOrEmpty(inputHash))
                return false;

            // bỏ 2 tham số hash trước khi build raw string
            _responseData.Remove("vnp_SecureHash");
            _responseData.Remove("vnp_SecureHashType");

            var sb = new StringBuilder();
            foreach (var kv in _responseData)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    continue;

                sb.Append(VnPayUrlEncode(kv.Key));
                sb.Append('=');
                sb.Append(VnPayUrlEncode(kv.Value));
                sb.Append('&');
            }

            if (sb.Length > 0)
                sb.Length -= 1;

            string rspRaw = sb.ToString();

            Console.WriteLine("========== VNPAY RESPONSE RAW ==========");
            Console.WriteLine(rspRaw);
            Console.WriteLine("=======================================");

            string myChecksum = Utils.HmacSHA512(secretKey, rspRaw);

            return myChecksum.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------------------------------------
        // URL ENCODE GIỐNG VNPay (space => '+')
        // -------------------------------------------------------
        private static string VnPayUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var encoded = WebUtility.UrlEncode(value);
            return encoded?.Replace("%20", "+");
        }
    }

    public static class Utils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(inputData))
                return string.Empty;

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);

            var hash = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                // HEX UPPERCASE – VNPay chơi được
                hash.Append(b.ToString("X2"));
            }

            return hash.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
