import random
from faker import Faker

fake = Faker()

brands = [
"RayBan","Oakley","Gucci","Prada","Dior",
"Versace","Police","Tom Ford","Burberry",
"Cartier","Armani","Calvin Klein"
]

images = [
"https://images.unsplash.com/photo-1511499767150-a48a237f0083",
"https://images.unsplash.com/photo-1517841905240-472988babdf9",
"https://images.unsplash.com/photo-1503342217505-b0a15ec3261c",
"https://images.unsplash.com/photo-1516826957135-700dedea698c",
"https://images.unsplash.com/photo-1492562080023-ab3db95bfbce",
"https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f"
]

sizes = ["S","M","L"]

start_id = 101   # bắt đầu từ SP101 để tránh trùng

sql_list = []

for i in range(300):

    maSP = f"SP{start_id+i:03}"

    brand = random.choice(brands)

    name = f"Kính {brand} {fake.word().capitalize()}"

    price = random.randint(300000,3000000)

    discount = random.choice([0,5,10,15,20])

    size = random.choice(sizes)

    gender = random.choice([0,1])

    image = random.choice(images)

    sql = f"""
INSERT INTO sanPham
(maSP,tenSP,hinhDD,moTaTomTat,ngayDang,moTa,taiKhoan,trangThai,giaBan,giamGia,maLoai,kichThuoc,maThuongHieu,gioiTinh)
VALUES
('{maSP}',N'{name}','{image}',
N'Kính thời trang cao cấp {brand}',
GETDATE(),
N'Sản phẩm kính chính hãng {brand}',
'admin',1,{price},{discount},1,N'{size}',1,{gender});
"""

    sql_list.append(sql)

with open("insert_300_products.sql","w",encoding="utf-8") as f:
    for s in sql_list:
        f.write(s)

print("Đã tạo 300 sản phẩm thành công!")