using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Text;
namespace MatKinh.Models
{
    public class HashPassword
    {
        public static string SHA512HashPass(string passwd)
        {
            string result = "";
            using (SHA512 sha512 = SHA512.Create())
            {
                // chuyển đổi chuỗi passwd thành mảng byte
                byte[] convert = Encoding.UTF8.GetBytes(passwd);
                // mã hóa mảng byte convert gán vào hashRS
                byte[] hashRS = sha512.ComputeHash(convert);
                //chuyển đổi mảng byte thành chuỗi
                result = BitConverter.ToString(hashRS);
            }
            return result;
        }
    }
}