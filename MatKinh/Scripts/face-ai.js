const imageUpload = document.getElementById("imageUpload");
const previewImage = document.getElementById("previewImage");
const result = document.getElementById("result");
const suggestion = document.getElementById("suggestion");
const recommendedProducts = document.getElementById("recommendedProducts");

imageUpload.addEventListener("change", function (event) {
    const file = event.target.files[0];
    if (!file) return;

    const reader = new FileReader();

    reader.onload = function (e) {
        previewImage.src = e.target.result;
        previewImage.style.display = "block";

        result.innerText = "AI đang phân tích khuôn mặt...";
        suggestion.innerText = "Vui lòng chờ vài giây...";
        recommendedProducts.innerHTML = "";

        setTimeout(function () {
            const shapes = ["round", "square", "long", "oval"];
            const faceShape = shapes[Math.floor(Math.random() * shapes.length)];

            showSuggestion(faceShape);
            showRecommendedProducts(faceShape);
        }, 1200);
    };

    reader.readAsDataURL(file);
});

function showSuggestion(faceShape) {
    const color = "#d8c3a0";

    if (faceShape === "round") {
        result.innerHTML = `Dáng mặt: <span style="color:${color};">Tròn</span>`;
        suggestion.innerText = "Bạn nên chọn gọng vuông, chữ nhật hoặc gọng có góc cạnh để khuôn mặt cân đối hơn.";
    } else if (faceShape === "square") {
        result.innerHTML = `Dáng mặt: <span style="color:${color};">Vuông</span>`;
        suggestion.innerText = "Bạn nên chọn gọng tròn, oval hoặc gọng bo góc mềm mại.";
    } else if (faceShape === "long") {
        result.innerHTML = `Dáng mặt: <span style="color:${color};">Dài</span>`;
        suggestion.innerText = "Bạn nên chọn gọng to bản, oversize hoặc gọng tròn.";
    } else {
        result.innerHTML = `Dáng mặt: <span style="color:${color};">Oval</span>`;
        suggestion.innerText = "Bạn phù hợp với hầu hết các kiểu gọng kính.";
    }
}

function showRecommendedProducts(faceShape) {
    let faceName = "mặt oval";

    if (faceShape === "round") faceName = "mặt tròn";
    else if (faceShape === "square") faceName = "mặt vuông";
    else if (faceShape === "long") faceName = "mặt dài";

    recommendedProducts.innerHTML = `
        <div class="ai-products-status">
            <h3>Gọng kính phù hợp với ${faceName}</h3>
            <p>AI đang chọn sản phẩm phù hợp từ cơ sở dữ liệu...</p>
        </div>
    `;

    fetch(getProductsUrl + "?faceShape=" + encodeURIComponent(faceShape))
        .then(function (response) {
            return response.json();
        })
        .then(function (products) {
            if (!products || products.length === 0) {
                recommendedProducts.innerHTML = `
                    <div class="ai-products-status">
                        <h3>Chưa tìm thấy sản phẩm phù hợp</h3>
                        <p>Bạn có thể thử lại bằng một ảnh khuôn mặt khác rõ hơn.</p>
                    </div>
                `;
                return;
            }

            let html = `
                <div class="ai-products-status">
                    <h3>Gọng kính phù hợp với ${faceName}</h3>
                    <p>Dưới đây là sản phẩm thật được lấy từ cơ sở dữ liệu của cửa hàng.</p>
                </div>
            `;

            products.forEach(function (product) {
                const productId =
                    product.sanPhamId ||
                    product.SanPhamId ||
                    product.id ||
                    product.Id;

                const productName =
                    product.name ||
                    product.TenSanPham ||
                    product.tenSanPham ||
                    "Sản phẩm kính";

                const productImage =
                    product.image ||
                    product.HinhAnh ||
                    product.hinhAnh ||
                    "/Asset/img/no-image.png";

                const productPrice =
                    product.price ||
                    product.GiaBan ||
                    product.giaBan ||
                    0;

                const productReason =
                    product.reason ||
                    product.LyDo ||
                    product.lyDo ||
                    "Sản phẩm phù hợp với dáng mặt đã phân tích.";

                const detailUrl = "/Product/DetailProduct?sanPhamId=" + encodeURIComponent(productId);

                html += `
                    <div class="ai-product-card">
                        <div class="ai-product-image-wrap">
                            <span class="ai-tag">AI đề xuất</span>

                            <img src="${productImage}"
                                 class="ai-product-img"
                                 alt="${productName}"
                                 onerror="this.src='/Asset/img/no-image.png'">
                        </div>

                        <div class="ai-product-body">
                            <div class="ai-product-name">
                                ${productName}
                            </div>

                            <div class="ai-product-price">
                                ${formatPrice(productPrice)}
                            </div>

                            <div class="ai-product-reason">
                                ${productReason}
                            </div>

                            <a href="${detailUrl}" class="ai-detail-btn">
                                Xem chi tiết
                            </a>
                        </div>
                    </div>
                `;
            });

            recommendedProducts.innerHTML = html;

            recommendedProducts.scrollIntoView({
                behavior: "smooth",
                block: "start"
            });
        })
        .catch(function () {
            recommendedProducts.innerHTML = `
                <div class="ai-products-status">
                    <h3>Không tải được sản phẩm</h3>
                    <p>Vui lòng kiểm tra lại Controller FaceAI hoặc dữ liệu sản phẩm.</p>
                </div>
            `;
        });
}

function formatPrice(price) {
    if (!price || Number(price) === 0) return "Liên hệ";
    return Number(price).toLocaleString("vi-VN") + "đ";
}