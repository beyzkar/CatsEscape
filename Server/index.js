const express = require('express');
const fs = require('fs');
const path = require('path');
const app = express();
app.use(express.json());

const DATA_FILE = path.join(__dirname, 'server_scores.json');
const MAX_SCORES = 10;

// Hafızadaki liste (Başlangıçta dosyadan yüklenir)
let scores = [];

// Skorları Dosyadan Yükle
function loadScores() {
    try {
        if (fs.existsSync(DATA_FILE)) {
            const data = fs.readFileSync(DATA_FILE, 'utf8');
            scores = JSON.parse(data);
            console.log(`[Sunucu] ${scores.length} adet gerçek skor başarıyla yüklendi.`);
        } else {
            console.log("[Sunucu] Skor dosyası bulunamadı, boş liste ile başlanıyor.");
            scores = [];
        }
    } catch (err) {
        console.error("[Sunucu] Dosya okuma hatası:", err);
        scores = [];
    }
}

// Skorları Dosyaya Kaydet
function saveScores() {
    try {
        // Her zaman en yüksek skorları en üstte tut ve sınırla
        scores.sort((a, b) => b.score - a.score);
        scores = scores.slice(0, MAX_SCORES);

        fs.writeFileSync(DATA_FILE, JSON.stringify(scores, null, 2));
    } catch (err) {
        console.error("Dosya kaydetme hatası:", err);
    }
}

// İlk açılışta yükle
loadScores();

// Sunucu Durum Kontrolü (Ana Sayfa)
app.get('/', (req, res) => {
    res.send("<h1>Kediler Kaçıyor Sunucusu Çalışıyor!</h1><p>Skorlar için <b>/leaderboard</b> adresine gidiniz.</p>");
});

// Skorları Çekme (GET /leaderboard)
app.get('/leaderboard', (req, res) => {
    console.log("Liderlik tablosu istendi.");
    res.json({ entries: scores });
});

// Skor Kaydetme (POST /scores)
app.post('/scores', (req, res) => {
    const data = req.body;

    if (data.playerName && data.score !== undefined) {
        console.log("Yeni skor geldi:", data.playerName, "-", data.score);
        scores.push({
            playerName: data.playerName,
            score: parseInt(data.score)
        });

        saveScores(); // Dosyaya yaz
        res.json({ success: true });
    } else {
        res.status(400).json({ success: false, message: "Hatalı veri!" });
    }
});

const port = 8080;
app.listen(port, () => {
    console.log('--- SUNUCU HAZIR (Kalıcı Mod) ---');
    console.log(`http://localhost:${port} adresinde seni bekliyorum.`);
});
