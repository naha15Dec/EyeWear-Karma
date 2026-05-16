let video = null;
let canvas = null;
let ctx = null;
let faceMesh = null;
let isFaceMeshReady = false;
let latestLandmarks = null;
let cameraStream = null;
let isAnalyzing = false;
let isCameraLoopRunning = false;

const MODEL_CONFIDENCE_THRESHOLD = 45;

const shapeNameMap = {
    ROUND: "Mặt tròn",
    OVAL: "Mặt oval",
    SQUARE: "Mặt vuông",
    HEART: "Mặt trái tim",
    LONG: "Mặt dài"
};

document.addEventListener("DOMContentLoaded", function () {
    video = document.getElementById("cameraVideo");
    canvas = document.getElementById("cameraCanvas");

    if (canvas) ctx = canvas.getContext("2d");

    const btnStartCamera = document.getElementById("btnStartCamera");
    const btnAnalyzeFace = document.getElementById("btnAnalyzeFace");
    const imageUpload = document.getElementById("imageUpload");

    if (btnAnalyzeFace) btnAnalyzeFace.disabled = true;

    if (btnStartCamera) btnStartCamera.addEventListener("click", startCamera);
    if (btnAnalyzeFace) btnAnalyzeFace.addEventListener("click", analyzeFromCamera);
    if (imageUpload) imageUpload.addEventListener("change", analyzeFromImage);
});

async function initFaceMesh() {
    if (isFaceMeshReady) return;

    setResult("Đang tải AI...", "Hệ thống đang khởi tạo MediaPipe Face Mesh.");

    faceMesh = new FaceMesh({
        locateFile: function (file) {
            return `https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh/${file}`;
        }
    });

    faceMesh.setOptions({
        maxNumFaces: 1,
        refineLandmarks: true,
        minDetectionConfidence: 0.5,
        minTrackingConfidence: 0.5
    });

    faceMesh.onResults(onFaceMeshResults);
    isFaceMeshReady = true;
}

async function startCamera() {
    if (cameraStream) {
        stopCamera();
        return;
    }

    try {
        await initFaceMesh();

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            setResult("Trình duyệt không hỗ trợ camera", "Hãy dùng Chrome hoặc Edge phiên bản mới.");
            return;
        }

        cameraStream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: "user",
                width: { ideal: 1280 },
                height: { ideal: 720 }
            },
            audio: false
        });

        video.srcObject = cameraStream;
        await video.play();

        hideElement("cameraPlaceholder");

        const preview = document.getElementById("previewImage");
        if (preview) {
            preview.style.display = "none";
            preview.src = "";
        }

        updateCameraButton(true);
        updateAnalyzeButton(true);

        setResult("Camera đã sẵn sàng", "Nhìn thẳng khuôn mặt, đủ sáng rồi bấm Phân tích khuôn mặt.");

        if (!isCameraLoopRunning) {
            isCameraLoopRunning = true;
            runMediaPipeLoop();
        }

    } catch (err) {
        console.error("Lỗi mở camera:", err);

        let message = "Không mở được camera.";

        if (err.name === "NotAllowedError") {
            message = "Bạn đã chặn quyền camera. Hãy bấm icon ổ khóa trên thanh địa chỉ và cho phép Camera.";
        } else if (err.name === "NotFoundError") {
            message = "Không tìm thấy camera trên thiết bị.";
        } else if (err.name === "NotReadableError") {
            message = "Camera đang bị ứng dụng khác sử dụng. Hãy tắt Zoom, Meet hoặc Camera app.";
        } else if (err.name === "SecurityError") {
            message = "Camera cần chạy trên localhost hoặc HTTPS.";
        }

        setResult("Không mở được camera", message);
    }
}

function stopCamera() {
    if (cameraStream) {
        cameraStream.getTracks().forEach(function (track) {
            track.stop();
        });
    }

    cameraStream = null;
    latestLandmarks = null;
    isCameraLoopRunning = false;

    if (video) {
        video.pause();
        video.srcObject = null;
    }

    if (ctx && canvas) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
    }

    updateCameraButton(false);
    updateAnalyzeButton(false);

    const preview = document.getElementById("previewImage");
    if (!preview || !preview.src) {
        showElement("cameraPlaceholder", "flex");
    }

    setResult("Camera đã tắt", "Bạn có thể bật camera hoặc chọn ảnh để AI phân tích.");
}

async function runMediaPipeLoop() {
    if (!video || !video.srcObject || !faceMesh || !cameraStream) {
        isCameraLoopRunning = false;
        return;
    }

    try {
        await faceMesh.send({ image: video });
    } catch (err) {
        console.error("MediaPipe loop error:", err);
    }

    requestAnimationFrame(runMediaPipeLoop);
}

function onFaceMeshResults(results) {
    if (!canvas || !ctx || !video) return;

    canvas.width = video.clientWidth;
    canvas.height = video.clientHeight;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    if (!results.multiFaceLandmarks || results.multiFaceLandmarks.length === 0) {
        latestLandmarks = null;
        return;
    }

    latestLandmarks = results.multiFaceLandmarks[0];
    drawSimpleFaceOutline(latestLandmarks);
}

async function analyzeFromCamera() {
    try {
        if (isAnalyzing) return;

        await initFaceMesh();

        if (!video || !video.srcObject) {
            setResult("Chưa bật camera", "Hãy bấm Bật camera trước khi phân tích.");
            return;
        }

        isAnalyzing = true;
        updateAnalyzeButton(false, "Đang phân tích...");

        const brightness = getBrightnessFromVideo(video);

        if (brightness < 28) {
            setResult(
                "Không đủ ánh sáng",
                "Khuôn mặt đang quá tối. Hãy bật đèn hoặc quay mặt về phía nguồn sáng rồi thử lại."
            );
            return;
        }

        setResult("AI đang quét khuôn mặt...", "Giữ mặt thẳng, không lắc đầu trong khoảng 3 giây.");

        const votes = {};
        let validFrames = 0;
        let rejectedFrames = 0;

        const targetFrames = 30;
        const delayPerFrame = 90;

        for (let i = 0; i < targetFrames; i++) {
            await sleep(delayPerFrame);

            if (!latestLandmarks) {
                rejectedFrames++;
                continue;
            }

            const frontalScore = getFrontalScore(latestLandmarks);

            if (frontalScore < 0.55) {
                rejectedFrames++;
                continue;
            }

            const shape = classifyFaceShape(latestLandmarks);
            votes[shape] = (votes[shape] || 0) + 1;
            validFrames++;
        }

        if (validFrames < 5) {
            setResult(
                "Chưa đủ dữ liệu khuôn mặt",
                "Hãy nhìn thẳng hơn, không lắc đầu quá nhiều, giữ đủ sáng rồi thử lại."
            );
            return;
        }

        const fallbackShape = getMajorityShape(votes);
        let fallbackConfidence = calculateConfidence(votes, fallbackShape, validFrames, rejectedFrames);

        if (brightness < 55) {
            fallbackConfidence = Math.max(55, fallbackConfidence - 8);
        }

        setResult("Đang gọi model AI...", "Hệ thống đang dùng model đã huấn luyện để phân tích dáng mặt.");

        const imageBlob = await captureVideoBlob();

        const modelResult = await predictByModel(imageBlob);

        if (isValidModelResult(modelResult)) {
            await getRecommendedProducts(
                modelResult.faceShape,
                modelResult.confidence,
                "MODEL"
            );
        } else {
            console.warn("Model fail hoặc confidence thấp, fallback FaceMesh:", modelResult);

            await getRecommendedProducts(
                fallbackShape,
                fallbackConfidence,
                "FALLBACK"
            );
        }

    } catch (err) {
        console.error(err);
        setResult("Có lỗi khi phân tích", "Vui lòng thử lại sau.");
    } finally {
        isAnalyzing = false;

        if (cameraStream) {
            updateAnalyzeButton(true);
        }
    }
}

async function analyzeFromImage(e) {
    try {
        if (isAnalyzing) return;

        await initFaceMesh();

        const file = e.target.files[0];
        if (!file) return;

        isAnalyzing = true;

        if (cameraStream) {
            stopCamera();
        }

        const img = await loadImageFromFile(file);

        const preview = document.getElementById("previewImage");
        if (preview) {
            preview.src = img.src;
            preview.style.display = "block";
        }

        hideElement("cameraPlaceholder");

        if (ctx && canvas) {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
        }

        updateAnalyzeButton(false);

        setResult("Đang gọi model AI...", "Hệ thống đang dùng model đã huấn luyện để phân tích ảnh.");

        const modelResult = await predictByModel(file);

        if (isValidModelResult(modelResult)) {
            await getRecommendedProducts(
                modelResult.faceShape,
                modelResult.confidence,
                "MODEL"
            );
            return;
        }

        setResult("Model chưa đủ chắc chắn...", "Hệ thống chuyển sang thuật toán dự phòng FaceMesh.");

        const imageResult = await analyzeImageWithVoting(img);

        if (!imageResult) {
            setResult("Không tìm thấy khuôn mặt", "Hãy chọn ảnh rõ mặt hơn, không bị che mặt và đủ sáng.");
            return;
        }

        await getRecommendedProducts(
            imageResult.shape,
            imageResult.confidence,
            "FALLBACK"
        );

    } catch (err) {
        console.error(err);
        setResult("Có lỗi khi phân tích ảnh", "Vui lòng chọn ảnh khác.");
    } finally {
        isAnalyzing = false;

        if (e && e.target) {
            e.target.value = "";
        }
    }
}

async function predictByModel(imageFileOrBlob) {
    try {
        if (!imageFileOrBlob) {
            return {
                success: false,
                message: "Không có ảnh gửi lên model."
            };
        }

        const url = typeof predictByModelUrl !== "undefined"
            ? predictByModelUrl
            : "/FaceAI/PredictByModel";

        const formData = new FormData();
        formData.append("image", imageFileOrBlob, "face.jpg");

        const response = await fetch(url, {
            method: "POST",
            body: formData
        });

        if (!response.ok) {
            return {
                success: false,
                message: "API model không phản hồi hợp lệ."
            };
        }

        return await response.json();

    } catch (err) {
        console.error("Predict model error:", err);

        return {
            success: false,
            message: "Không gọi được model AI."
        };
    }
}

function isValidModelResult(data) {
    if (!data || data.success !== true) return false;

    const shape = String(data.faceShape || "").toUpperCase();
    const confidence = Number(data.confidence || 0);

    const validShapes = ["ROUND", "OVAL", "SQUARE", "HEART", "LONG"];

    return validShapes.includes(shape) && confidence >= MODEL_CONFIDENCE_THRESHOLD;
}

function captureVideoBlob() {
    return new Promise(function (resolve, reject) {
        if (!video || video.videoWidth === 0 || video.videoHeight === 0) {
            reject("Video chưa sẵn sàng.");
            return;
        }

        const c = document.createElement("canvas");
        const cctx = c.getContext("2d");

        c.width = video.videoWidth;
        c.height = video.videoHeight;

        cctx.drawImage(video, 0, 0, c.width, c.height);

        c.toBlob(function (blob) {
            if (blob) resolve(blob);
            else reject("Không tạo được ảnh từ camera.");
        }, "image/jpeg", 0.92);
    });
}

function analyzeStaticImage(img) {
    return new Promise(async function (resolve) {
        const tempFaceMesh = new FaceMesh({
            locateFile: function (file) {
                return `https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh/${file}`;
            }
        });

        tempFaceMesh.setOptions({
            maxNumFaces: 1,
            refineLandmarks: true,
            minDetectionConfidence: 0.5,
            minTrackingConfidence: 0.5
        });

        tempFaceMesh.onResults(function (results) {
            if (results.multiFaceLandmarks && results.multiFaceLandmarks.length > 0) {
                resolve({
                    landmarks: results.multiFaceLandmarks[0]
                });
            } else {
                resolve(null);
            }
        });

        await tempFaceMesh.send({ image: img });
    });
}

async function analyzeImageWithVoting(img) {
    const votes = {};
    let validRuns = 0;
    let rejectedRuns = 0;

    const variants = createImageVariants(img);

    for (const variant of variants) {
        const result = await analyzeStaticImage(variant);

        if (!result || !result.landmarks) {
            rejectedRuns++;
            continue;
        }

        const frontalScore = getFrontalScore(result.landmarks);

        if (frontalScore < 0.48) {
            rejectedRuns++;
            continue;
        }

        const shape = classifyFaceShape(result.landmarks);
        votes[shape] = (votes[shape] || 0) + 1;
        validRuns++;
    }

    if (validRuns === 0) return null;

    const finalShape = getMajorityShape(votes);
    const confidence = calculateConfidence(votes, finalShape, validRuns, rejectedRuns);

    return {
        shape: finalShape,
        confidence: confidence,
        votes: votes
    };
}

function createImageVariants(img) {
    const variants = [];

    const configs = [
        { scale: 1.00, dx: 0, dy: 0 },
        { scale: 1.02, dx: 0, dy: 0 },
        { scale: 1.05, dx: 0, dy: 0 },
        { scale: 0.98, dx: 0, dy: 0 },
        { scale: 1.03, dx: -8, dy: 0 },
        { scale: 1.03, dx: 8, dy: 0 },
        { scale: 1.03, dx: 0, dy: -8 },
        { scale: 1.03, dx: 0, dy: 8 }
    ];

    configs.forEach(function (cfg) {
        const c = document.createElement("canvas");
        const cctx = c.getContext("2d");

        c.width = img.naturalWidth || img.width;
        c.height = img.naturalHeight || img.height;

        cctx.fillStyle = "#ffffff";
        cctx.fillRect(0, 0, c.width, c.height);

        const newWidth = c.width * cfg.scale;
        const newHeight = c.height * cfg.scale;

        const x = (c.width - newWidth) / 2 + cfg.dx;
        const y = (c.height - newHeight) / 2 + cfg.dy;

        cctx.drawImage(img, x, y, newWidth, newHeight);
        variants.push(c);
    });

    return variants;
}

function classifyFaceShape(lm) {
    const foreheadLeft = lm[54];
    const foreheadRight = lm[284];

    const cheekLeft = lm[234];
    const cheekRight = lm[454];

    const jawLeft = lm[172];
    const jawRight = lm[397];

    const chinLeft = lm[148];
    const chinRight = lm[377];

    const top = lm[10];
    const chin = lm[152];

    const faceHeight = distance(top, chin);
    const cheekWidth = distance(cheekLeft, cheekRight);
    const foreheadWidth = distance(foreheadLeft, foreheadRight);
    const jawWidth = distance(jawLeft, jawRight);
    const chinWidth = distance(chinLeft, chinRight);

    if (cheekWidth === 0) return "OVAL";

    const heightRatio = faceHeight / cheekWidth;
    const jawRatio = jawWidth / cheekWidth;
    const foreheadRatio = foreheadWidth / cheekWidth;
    const chinRatio = chinWidth / cheekWidth;

    const scores = {
        ROUND: 0,
        OVAL: 0,
        SQUARE: 0,
        HEART: 0,
        LONG: 0
    };

    if (heightRatio >= 1.50) scores.LONG += 4;
    else if (heightRatio >= 1.42) scores.LONG += 2;

    if (heightRatio <= 1.24) scores.ROUND += 4;
    else if (heightRatio <= 1.32) scores.ROUND += 2;

    if (jawRatio >= 0.75 && jawRatio <= 0.90) scores.ROUND += 1;
    if (chinRatio < 0.56) scores.ROUND += 1;

    if (jawRatio >= 0.84) scores.SQUARE += 3;
    if (chinRatio >= 0.50) scores.SQUARE += 2;
    if (heightRatio >= 1.18 && heightRatio <= 1.43) scores.SQUARE += 1;

    if (foreheadRatio >= 0.88) scores.HEART += 2;
    if (jawRatio <= 0.80) scores.HEART += 2;
    if (chinRatio <= 0.48) scores.HEART += 3;

    if (heightRatio > 1.26 && heightRatio < 1.50) scores.OVAL += 3;
    if (jawRatio > 0.74 && jawRatio < 0.90) scores.OVAL += 2;
    if (chinRatio > 0.40 && chinRatio < 0.58) scores.OVAL += 1;

    return getBestScoreShape(scores);
}

function getBestScoreShape(scores) {
    let bestShape = "OVAL";
    let bestScore = scores.OVAL;

    Object.keys(scores).forEach(function (shape) {
        if (scores[shape] > bestScore) {
            bestShape = shape;
            bestScore = scores[shape];
        }
    });

    return bestShape;
}

function getFrontalScore(lm) {
    const nose = lm[1];
    const leftCheek = lm[234];
    const rightCheek = lm[454];

    const leftDist = distance(nose, leftCheek);
    const rightDist = distance(nose, rightCheek);

    if (leftDist === 0 || rightDist === 0) return 0;

    return Math.min(leftDist, rightDist) / Math.max(leftDist, rightDist);
}

function getBrightnessFromVideo(videoElement) {
    if (!videoElement || videoElement.videoWidth === 0 || videoElement.videoHeight === 0) {
        return 100;
    }

    const c = document.createElement("canvas");
    const cctx = c.getContext("2d");

    c.width = 120;
    c.height = 90;

    try {
        cctx.drawImage(videoElement, 0, 0, c.width, c.height);
        const imageData = cctx.getImageData(0, 0, c.width, c.height).data;

        let total = 0;
        let count = 0;

        for (let i = 0; i < imageData.length; i += 16) {
            const r = imageData[i];
            const g = imageData[i + 1];
            const b = imageData[i + 2];

            total += (r + g + b) / 3;
            count++;
        }

        return total / count;
    } catch (err) {
        console.error("Brightness check error:", err);
        return 100;
    }
}

function calculateConfidence(votes, finalShape, validCount, rejectedCount) {
    if (!votes || !finalShape || !validCount || validCount === 0) {
        return 60;
    }

    const voteRate = votes[finalShape] / validCount;
    let confidence = Math.round(voteRate * 100);

    if (rejectedCount && rejectedCount > 0) {
        confidence -= Math.min(12, rejectedCount);
    }

    if (confidence > 94) confidence = 94;
    if (confidence < 55) confidence = 55;

    return confidence;
}

function getMajorityShape(votes) {
    let bestShape = "OVAL";
    let bestCount = 0;

    Object.keys(votes).forEach(function (shape) {
        if (votes[shape] > bestCount) {
            bestShape = shape;
            bestCount = votes[shape];
        }
    });

    return bestShape;
}

async function getRecommendedProducts(faceShape, confidence, source) {
    const response = await fetch(`${getProductsUrl}?faceShape=${encodeURIComponent(faceShape)}`);
    const data = await response.json();

    const faceName = shapeNameMap[faceShape] || faceShape;
    const sourceText = source === "MODEL" ? "model AI đã huấn luyện" : "thuật toán dự phòng FaceMesh";

    if (!data.success) {
        setResult("Không thể gợi ý", data.message || "Có lỗi xảy ra.");
        return;
    }

    setResult(
        faceName,
        `AI nhận diện bạn có ${faceName.toLowerCase()} bằng ${sourceText}, độ tin cậy khoảng ${confidence}%.`
    );

    document.getElementById("recommendedTitle").innerText = `Gợi ý cho ${faceName}`;

    document.getElementById("recommendedDesc").innerText =
        data.isFallback
            ? "Chưa có sản phẩm khớp hoàn toàn, hệ thống đang hiển thị sản phẩm nổi bật."
            : "Các sản phẩm dưới đây được chọn theo kiểu gọng phù hợp với dáng mặt của bạn.";

    renderProducts(data.products);
}

function renderProducts(products) {
    const box = document.getElementById("recommendedProducts");
    if (!box) return;

    if (!products || products.length === 0) {
        box.innerHTML = `
            <div class="ai-products-status">
                <h3>Chưa có sản phẩm phù hợp</h3>
                <p>Vui lòng thử lại hoặc chọn dáng mặt khác.</p>
            </div>
        `;
        return;
    }

    box.innerHTML = products.map(function (p) {
        return `
            <div class="ai-product-card">
                <div class="ai-product-image-wrap">
                    <span class="ai-tag">${escapeHtml(p.frameName || "Gợi ý AI")}</span>

                    <img
                        class="ai-product-img"
                        src="${escapeHtml(p.image || "/Content/images/no-image.png")}"
                        alt="${escapeHtml(p.name || "Sản phẩm kính")}"
                        onerror="this.src='/Content/images/no-image.png'"
                    >
                </div>

                <div class="ai-product-body">
                    <div class="ai-product-name">${escapeHtml(p.name || "Sản phẩm kính")}</div>

                    <div class="ai-product-price">
                        ${formatMoney(p.price)}
                    </div>

                    <div class="ai-product-reason">
                        ${escapeHtml(p.reason || "")}
                    </div>

                    <a
                        href="/Product/DetailProduct?sanPhamId=${encodeURIComponent(p.id)}"
                        class="ai-detail-btn"
                    >
                        Xem chi tiết
                    </a>
                </div>
            </div>
        `;
    }).join("");
}

function drawSimpleFaceOutline(lm) {
    if (!ctx || !canvas) return;

    const points = [
        lm[10],
        lm[338],
        lm[454],
        lm[397],
        lm[152],
        lm[172],
        lm[234],
        lm[109]
    ];

    ctx.beginPath();

    points.forEach(function (p, index) {
        const x = p.x * canvas.width;
        const y = p.y * canvas.height;

        if (index === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
    });

    ctx.closePath();
    ctx.lineWidth = 3;
    ctx.strokeStyle = "#d8c3a0";
    ctx.stroke();
}

function loadImageFromFile(file) {
    return new Promise(function (resolve, reject) {
        const img = new Image();
        const objectUrl = URL.createObjectURL(file);

        img.onload = function () {
            resolve(img);
        };

        img.onerror = function () {
            URL.revokeObjectURL(objectUrl);
            reject();
        };

        img.src = objectUrl;
    });
}

function distance(p1, p2) {
    if (!p1 || !p2) return 0;

    const dx = p1.x - p2.x;
    const dy = p1.y - p2.y;

    return Math.sqrt(dx * dx + dy * dy);
}

function sleep(ms) {
    return new Promise(function (resolve) {
        setTimeout(resolve, ms);
    });
}

function setResult(title, desc) {
    const result = document.getElementById("result");
    const suggestion = document.getElementById("suggestion");

    if (result) result.innerText = title;
    if (suggestion) suggestion.innerText = desc;
}

function updateCameraButton(isOn) {
    const btn = document.getElementById("btnStartCamera");
    if (!btn) return;

    if (isOn) {
        btn.innerHTML = `<i class="fa fa-stop"></i> Tắt camera`;
        btn.classList.add("is-active");
    } else {
        btn.innerHTML = `<i class="fa fa-video-camera"></i> Bật camera`;
        btn.classList.remove("is-active");
    }
}

function updateAnalyzeButton(enabled, text) {
    const btn = document.getElementById("btnAnalyzeFace");
    if (!btn) return;

    btn.disabled = !enabled;

    if (text) {
        btn.innerHTML = `<i class="fa fa-spinner fa-spin"></i> ${text}`;
    } else {
        btn.innerHTML = `<i class="fa fa-magic"></i> Phân tích khuôn mặt`;
    }
}

function formatMoney(value) {
    if (!value || Number(value) === 0) return "Liên hệ";
    return Number(value).toLocaleString("vi-VN") + "đ";
}

function hideElement(id) {
    const el = document.getElementById(id);
    if (el) el.style.display = "none";
}

function showElement(id, displayType) {
    const el = document.getElementById(id);
    if (el) el.style.display = displayType || "block";
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}