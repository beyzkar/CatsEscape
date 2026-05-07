require('dotenv').config();
const express = require('express');
const mongoose = require('mongoose');
const cors = require('cors');
const admin = require('firebase-admin');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 5001;

// Middleware
app.use(cors());
app.use(express.json());

// Request Logging Middleware
app.use((req, res, next) => {
  console.log("------ NEW REQUEST ------");
  console.log("Time:", new Date().toISOString());
  console.log("URL:", req.method, req.url);
  console.log("Headers:", JSON.stringify(req.headers, null, 2));
  console.log("Body:", JSON.stringify(req.body, null, 2));
  next();
});

// Firebase Admin Initialization
const serviceAccountPath = process.env.FIREBASE_SERVICE_ACCOUNT_PATH || './serviceAccountKey.json';
const absolutePath = path.resolve(serviceAccountPath);

if (fs.existsSync(absolutePath)) {
  const serviceAccount = require(absolutePath);
  admin.initializeApp({
    credential: admin.credential.cert(serviceAccount)
  });
  console.log('[Backend] Firebase Admin initialized with service account.');
} else {
  console.warn('[Backend] Firebase service account key not found at:', absolutePath);
  console.warn('[Backend] API calls requiring authentication will fail.');
}

// MongoDB Connection
const mongodbUri = process.env.MONGODB_URI || 'mongodb://127.0.0.1:27017/catsescape';
console.log(`[Backend] Attempting to connect to MongoDB...`);

mongoose.connect(mongodbUri)
  .then(async () => {
    console.log('[Backend] Successfully connected to MongoDB.');
    
    // AUTO-FIX: Drop ALL potential unique indexes on the leaderboard collection
    try {
      const LeaderboardScore = require('./models/LeaderboardScore');
      const indexes = await LeaderboardScore.collection.indexes();
      
      const indexesToDrop = ["uid_1", "uid_1_levelNumber_1", "userName_1"];
      
      for (const indexName of indexesToDrop) {
        if (indexes.some(idx => idx.name === indexName)) {
          console.log(`[Backend] Found legacy unique index '${indexName}'. Dropping it...`);
          await LeaderboardScore.collection.dropIndex(indexName);
          console.log(`[Backend] Index '${indexName}' dropped successfully.`);
        }
      }

      // Ensure PlayerProfile unique indexes
      const PlayerProfile = require('./models/PlayerProfile');
      await PlayerProfile.collection.createIndex({ userNameNormalized: 1 }, { unique: true, sparse: true });
      console.log('[Backend] PlayerProfile indexes verified.');

    } catch (indexErr) {
      console.warn('[Backend] Index check/drop skipped (already clean):', indexErr.message);
    }
  })
  .catch(err => {
    console.error('[Backend] MongoDB connection error:');
    console.error(err);
  });

// Routes
app.use('/api/game', require('./routes/gameRoutes'));

// Health check
app.get('/health', (req, res) => {
  res.status(200).json({ status: 'ok', timestamp: new Date() });
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`[Backend] Server running on port ${PORT} (Listening on all interfaces)`);
});
