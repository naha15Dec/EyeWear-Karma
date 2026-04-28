const imageUpload = document.getElementById("imageUpload");
const previewImage = document.getElementById("previewImage");
const result = document.getElementById("result");
const suggestion = document.getElementById("suggestion");
const recommendedProducts = document.getElementById("recommendedProducts");

imageUpload.addEventListener("change", function (event) {
    const file = event.target.files[0];

    if (!file) return;

    const imageUrl = URL.createObjectURL(file);

    previewImage.src = imageUrl;
    previewImage.style.display = "block";
    previewImage.style.opacity = "0";

    setTimeout(function () {
        previewImage.style.transition = "0.5s";
        previewImage.style.opacity = "1";
    }, 50);

    result.innerText = "AI đang phân tích khuôn mặt...";
    suggestion.innerText = "Vui lòng chờ vài giây...";

    previewImage.onload = async function () {
        await detectFaceShape(previewImage);
    };
});

async function detectFaceShape(imageElement) {
    const faceMesh = new FaceMesh({
        locateFile: function (file) {
            return "https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh/" + file;
        }
    });

    faceMesh.setOptions({
        maxNumFaces: 1,
        refineLandmarks: true,
        minDetectionConfidence: 0.5,
        minTrackingConfidence: 0.5
    });

    faceMesh.onResults(function (results) {
        if (!results.multiFaceLandmarks || results.multiFaceLandmarks.length === 0) {
            result.innerText = "Không phát hiện được khuôn mặt.";
            suggestion.innerText = "Vui lòng chọn ảnh rõ mặt, nhìn thẳng và đủ sáng.";
            return;
        }

        const landmarks = results.multiFaceLandmarks[0];
        const faceShape = classifyFaceShape(landmarks);

        showSuggestion(faceShape);
        showRecommendedProducts(faceShape);
    });

    await faceMesh.send({ image: imageElement });
}

function distance(point1, point2) {
    const dx = point1.x - point2.x;
    const dy = point1.y - point2.y;
    return Math.sqrt(dx * dx + dy * dy);
}

function classifyFaceShape(landmarks) {
    const forehead = landmarks[10];
    const chin = landmarks[152];
    const leftCheek = landmarks[234];
    const rightCheek = landmarks[454];
    const leftJaw = landmarks[172];
    const rightJaw = landmarks[397];

    const faceLength = distance(forehead, chin);
    const cheekWidth = distance(leftCheek, rightCheek);
    const jawWidth = distance(leftJaw, rightJaw);

    const ratio = faceLength / cheekWidth;

    if (ratio > 1.45) {
        return "long";
    }

    if (Math.abs(jawWidth - cheekWidth) < 0.04 && ratio < 1.25) {
        return "square";
    }

    if (ratio < 1.25) {
        return "round";
    }

    return "oval";
}

function showSuggestion(faceShape) {
    if (faceShape === "round") {
        result.innerHTML = "Dáng mặt: <span style='color:#fbb72c;'>Tròn</span>";
        suggestion.innerText = "Bạn nên chọn gọng vuông, chữ nhật hoặc gọng có góc cạnh để khuôn mặt cân đối hơn.";
    } else if (faceShape === "square") {
        result.innerHTML = "Dáng mặt: <span style='color:#fbb72c;'>Vuông</span>";
        suggestion.innerText = "Bạn nên chọn gọng tròn, oval hoặc gọng bo góc mềm mại.";
    } else if (faceShape === "long") {
        result.innerHTML = "Dáng mặt: <span style='color:#fbb72c;'>Dài</span>";
        suggestion.innerText = "Bạn nên chọn gọng to bản, oversize hoặc gọng tròn.";
    } else {
        result.innerHTML = "Dáng mặt: <span style='color:#fbb72c;'>Oval</span>";
        suggestion.innerText = "Bạn phù hợp với hầu hết các kiểu gọng kính.";
    }
}

function showRecommendedProducts(faceShape) {
    recommendedProducts.innerHTML = "<p>Đang tải sản phẩm gợi ý từ cơ sở dữ liệu...</p>";

    let faceName = "";

    if (faceShape === "round") faceName = "mặt tròn";
    else if (faceShape === "square") faceName = "mặt vuông";
    else if (faceShape === "long") faceName = "mặt dài";
    else faceName = "mặt oval";

    fetch(getProductsUrl + "?faceShape=" + faceShape)
        .then(function (response) {
            return response.json();
        })
        .then(function (products) {
            recommendedProducts.innerHTML = `
                <div style="width:100%; text-align:center; margin-bottom:20px;">
                    <h3 style="font-size:30px; font-weight:800; color:#222;">
                        Gọng kính phù hợp với ${faceName}
                    </h3>
                   
                </div>
            `;

            if (!products || products.length === 0) {
                recommendedProducts.innerHTML += "<p>Chưa tìm thấy sản phẩm phù hợp.</p>";
                return;
            }

            products.forEach(function (product) {
                recommendedProducts.innerHTML += `
                    <div style="
                            width:100%;
                            background:#fff;
                            border-radius:22px;
                            overflow:hidden;
                            box-shadow:0 16px 35px rgba(0,0,0,0.12);
                            text-align:center;
                            position:relative;
                            border:1px solid #f1f1f1;
                        ">
                        <div style="
                            position:absolute;
                            top:12px;
                            left:12px;
                            background:#fbb72c;
                            color:#fff;
                            padding:5px 10px;
                            font-size:12px;
                            font-weight:700;
                            border-radius:20px;
                            z-index:2;
                        ">AI đề xuất</div>

                        <img src="${product.image}" 
                             style="width:100%; height:180px; object-fit:cover;">

                        <div style="padding:20px;">
                            <h4 style="font-size:17px; font-weight:700; color:#222; min-height:50px;">
                                ${product.name}
                            </h4>

                            <p style="color:#f5a400; font-weight:800; font-size:18px;">
                                ${Number(product.price).toLocaleString("vi-VN")}đ
                            </p>

                            <a href="/Product/Detail/${product.id}" style="
                                display:block;
                                background:linear-gradient(135deg,#fbb72c,#ff9900);
                                color:#fff;
                                padding:11px;
                                border-radius:12px;
                                text-decoration:none;
                                font-weight:700;
                            ">Xem chi tiết</a>
                        </div>
                    </div>
                `;
            });

            recommendedProducts.scrollIntoView({
                behavior: "smooth",
                block: "start"
            });
        })
        .catch(function () {
            recommendedProducts.innerHTML = "<p>Không tải được sản phẩm gợi ý từ cơ sở dữ liệu.</p>";
        });
}