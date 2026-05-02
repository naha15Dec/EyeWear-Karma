using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MatKinh.Services
{
    public static class RecommendationService
    {
        private static readonly string BaseUrl = "http://localhost:5555/";

        public static async Task<List<int>> GetPersonalized(int? khachHangId, string sessionId)
        {
            List<int> result = new List<int>();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(5);

                    string url;

                    if (khachHangId.HasValue && khachHangId.Value > 0)
                    {
                        url = $"api/personalized?khach_hang_id={khachHangId.Value}";
                    }
                    else
                    {
                        url = $"api/personalized?session_id={sessionId}";
                    }

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        return result;
                    }

                    string content = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(content);

                    if (data != null && data.data != null)
                    {
                        foreach (var item in data.data)
                        {
                            int id;
                            if (int.TryParse(item.ToString(), out id))
                            {
                                result.Add(id);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        public static async Task<List<int>> GetSimilar(int sanPhamId)
        {
            List<int> result = new List<int>();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(5);

                    HttpResponseMessage response = await client.GetAsync($"api/similar?product_id={sanPhamId}");

                    if (!response.IsSuccessStatusCode)
                    {
                        return result;
                    }

                    string content = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(content);

                    if (data != null && data.data != null)
                    {
                        foreach (var item in data.data)
                        {
                            int id;
                            if (int.TryParse(item.ToString(), out id))
                            {
                                result.Add(id);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return result;
        }
    }
}