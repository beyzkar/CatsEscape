const express = require('express');
const router = express.Router();
const LevelResult = require('../models/LevelResult');
const PlayerProfile = require('../models/PlayerProfile');
const PlayerActivity = require('../models/PlayerActivity');
const LeaderboardScore = require('../models/LeaderboardScore');
const verifyToken = require('../middleware/auth');

// POST /api/game/level-result
router.post('/level-result', verifyToken, async (req, res) => {
  try {
    const {
      userName,
      levelNumber,
      levelResult,
      xpEarned,
      totalXP,
      fishSpawnCount,
      potionSpawnCount,
      heartsGained,
      heartsLost,
      deviceInfo,
      startedAt,
      completedAt,
      abandonedAt,
      durationSeconds
    } = req.body;

    const newResult = new LevelResult({
      uid: req.user.uid,
      userName,
      levelNumber,
      levelResult,
      xpEarned,
      fishSpawnCount,
      potionSpawnCount,
      heartsGained,
      heartsLost,
      deviceInfo,
      startedAt: startedAt ? new Date(startedAt) : null,
      completedAt: completedAt ? new Date(completedAt) : (levelResult === 'completed' ? new Date() : null),
      abandonedAt: abandonedAt ? new Date(abandonedAt) : (levelResult === 'abandoned' ? new Date() : null),
      durationSeconds
    });

    await newResult.save();
    console.log(`[Backend] Saved level result (${levelResult}) for user ${req.user.uid}, level ${levelNumber}`);

    // Update Player Profile
    const isCompleted = (levelResult === 'completed');
    const isFailed = (levelResult === 'failed');
    const isAbandoned = (levelResult === 'abandoned');

    const updateData = {
      $inc: {
        totalCompletions: isCompleted ? 1 : 0,
        totalFailures: isFailed ? 1 : 0,
        totalAbandoned: isAbandoned ? 1 : 0
      },
      $max: {
        highestXP: totalXP || 0
      },
      lastActiveAt: new Date()
    };

    if (userName) updateData.$set = { userName };

    if (isCompleted) {
      updateData.lastLevelReached = levelNumber + 1;
      updateData.$max.highestLevelReached = levelNumber + 1;
    }

    await PlayerProfile.findOneAndUpdate(
      { uid: req.user.uid },
      updateData,
      { upsert: true, new: true }
    );

    res.status(201).json({ message: 'Level result saved successfully', data: newResult });
  } catch (error) {
    console.error('[Backend] Error saving level result:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

// POST /api/game/activity
router.post('/activity', verifyToken, async (req, res) => {
  try {
    const { eventType, levelNumber, result, sessionId, userName } = req.body;

    if (!eventType || !sessionId) {
      return res.status(400).json({ message: 'eventType and sessionId are required' });
    }

    const newActivity = new PlayerActivity({
      uid: req.user.uid,
      userName,
      eventType,
      levelNumber: levelNumber !== undefined ? levelNumber : null,
      result: result || null,
      sessionId
    });

    await newActivity.save();
    console.log(`[Backend] Saved activity for user ${req.user.uid}: ${eventType}`);

    res.status(201).json({ message: 'Activity saved successfully', data: newActivity });
  } catch (error) {
    console.error('[Backend] Error saving activity:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

// POST /api/game/scores
router.post('/scores', verifyToken, async (req, res) => {
  try {
    const { displayName, authType, levelNumber, score, xpEarned, timeSeconds, userName } = req.body;
    const uid = req.user.uid;

    console.log(`[LEADERBOARD_SAVE] Submit Start UID=${uid} UserName=${userName} Score=${score} Level=${levelNumber}`);

    if (levelNumber === undefined || score === undefined) {
      return res.status(400).json({ success: false, message: 'levelNumber and score are required' });
    }

    // 1. Create a NEW leaderboard entry (Attempt-based, NO UNIQUE UID CONSTRAINT)
    // We use LeaderboardScore.create() or new ().save() to ensure an INSERT operation.
    const newScoreEntry = new LeaderboardScore({
      uid,
      userName,
      displayName: displayName || 'Player',
      authType: authType || 'guest',
      levelNumber: Number(levelNumber),
      score: Number(score),
      xpEarned: xpEarned || 0,
      timeSeconds: timeSeconds || 0
    });

    await newScoreEntry.save();
    console.log(`[LEADERBOARD_SAVE] Success. New Doc ID: ${newScoreEntry._id}`);

    // 2. Separately update PlayerProfile.highestXP (This stays unique per user)
    await PlayerProfile.findOneAndUpdate(
      { uid },
      {
        $max: { highestXP: Number(score) },
        $set: { lastActiveAt: new Date() }
      },
      { upsert: true }
    );

    return res.status(201).json({
      success: true,
      message: 'Score saved successfully',
      data: newScoreEntry
    });

  } catch (error) {
    console.error('[LEADERBOARD_SAVE] Critical Error:', error);
    // If we get a Duplicate Key error (11000), it's because of a MongoDB index restriction
    if (error.code === 11000) {
      return res.status(409).json({
        success: false,
        message: 'DATABASE_INDEX_ERROR: Duplicate entry detected. Please drop unique index on uid in MongoDB.',
        error: error.message
      });
    }
    res.status(500).json({ success: false, message: 'Server error', error: error.message });
  }
});

// GET /api/game/leaderboard
router.get('/leaderboard', verifyToken, async (req, res) => {
  try {
    const { levelNumber } = req.query;

    let query = {};
    if (levelNumber) {
      query.levelNumber = parseInt(levelNumber);
    }

    // Return TOP 5 scores of all time (Multiple entries per user allowed)
    const entries = await LeaderboardScore.find(query)
      .sort({ score: -1, createdAt: 1 })
      .limit(5);

    console.log(`[LEADERBOARD] Fetching top 5 scores. Found: ${entries.length} entries.`);

    res.status(200).json({
      success: true,
      count: entries.length,
      entries
    });
  } catch (error) {
    console.error('[LEADERBOARD] Error fetching leaderboard:', error);
    res.status(500).json({ success: false, message: 'Server error', error: error.message });
  }
});

// GET /api/game/progress
router.get('/progress', verifyToken, async (req, res) => {
  try {
    const profile = await PlayerProfile.findOne({ uid: req.user.uid });
    if (!profile) {
      return res.status(404).json({ message: 'Player profile not found' });
    }
    res.status(200).json(profile);
  } catch (error) {
    console.error('[Backend] Error fetching progress:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

// GET /api/game/user/next-guest-username
router.get('/user/next-guest-username', verifyToken, async (req, res) => {
  try {
    const guests = await PlayerProfile.find({ userName: /^Guest\d+$/ }, { userName: 1 });
    let max = 0;
    guests.forEach(g => {
      const match = g.userName.match(/^Guest(\d+)$/);
      if (match) {
        const num = parseInt(match[1]);
        if (num > max) max = num;
      }
    });
    res.status(200).json({ nextGuestName: `Guest${max + 1}` });
  } catch (error) {
    console.error('[Backend] Error calculating next guest name:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

// POST /api/game/user/set-username
router.post('/user/set-username', verifyToken, async (req, res) => {
  try {
    const { userName } = req.body;
    const uid = req.user.uid;

    // Safely derive provider and authType from Firebase token
    const provider = req.user.firebase?.sign_in_provider || "anonymous";
    const authType = (provider === "google.com") ? "google" : "guest";

    // Safely derive displayName from Firebase token
    let displayName = req.user.name || (req.user.email ? req.user.email.split('@')[0] : "Player");
    if (authType === "guest") displayName = "Guest";

    console.log(`[AUTH] uid: ${uid}`);
    console.log(`[AUTH] provider: ${provider}`);
    console.log(`[AUTH] resolved authType: ${authType}`);
    console.log(`[AUTH] displayName: ${displayName}`);
    console.log(`[AUTH] requested userName: ${userName}`);

    if (!userName || userName.trim() === "") {
      return res.status(400).json({ success: false, message: 'UserName cannot be empty' });
    }

    const trimmedName = userName.trim();
    const normalizedName = trimmedName.toLowerCase();

    // Check if username is already taken (Case-insensitive via normalized field)
    const existing = await PlayerProfile.findOne({
      userNameNormalized: normalizedName
    });

    if (existing && existing.uid !== uid) {
      return res.status(400).json({ success: false, message: 'USERNAME_TAKEN' });
    }

    // Update with $set for persistent identity, $setOnInsert for new players
    const updatedProfile = await PlayerProfile.findOneAndUpdate(
      { uid: uid },
      {
        $set: {
          userName: trimmedName,
          userNameNormalized: normalizedName,
          authType: authType,
          displayName: displayName,
          lastActiveAt: new Date(),
          updatedAt: new Date()
        },
        $setOnInsert: {
          highestXP: 0,
          highestLevelReached: 1,
          lastLevelReached: 1,
          totalCompletions: 0,
          totalFailures: 0,
          totalAbandoned: 0,
          createdAt: new Date()
        }
      },
      { new: true, upsert: true, setDefaultsOnInsert: true }
    );

    console.log(`[AUTH] Mongo Result: Saved profile for ${uid} as ${authType}`);

    res.status(200).json({
      success: true,
      profile: updatedProfile
    });

  } catch (error) {
    console.error('[AUTH] Critical Error:', error);
    if (error.code === 11000) {
      return res.status(400).json({ success: false, message: 'USERNAME_TAKEN' });
    }
    res.status(500).json({ success: false, message: 'SERVER_ERROR', error: error.message });
  }
});

// GET /api/game/history
router.get('/history', verifyToken, async (req, res) => {
  try {
    const results = await LevelResult.find({ uid: req.user.uid }).sort({ completedAt: -1 }).limit(20);
    res.status(200).json(results);
  } catch (error) {
    console.error('[Backend] Error fetching history:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

// POST /api/game/profile/init
router.post('/profile/init', verifyToken, async (req, res) => {
  try {
    const uid = req.user.uid;

    // Safely derive provider and authType from Firebase token
    const provider = req.user.firebase?.sign_in_provider || "anonymous";
    const authType = (provider === "google.com") ? "google" : "guest";

    // Safely derive displayName from Firebase token
    let displayName = req.user.name || (req.user.email ? req.user.email.split('@')[0] : "Player");
    if (authType === "guest") displayName = "Guest";

    console.log(`[AUTH_INIT] uid: ${uid}`);
    console.log(`[AUTH_INIT] provider: ${provider}`);
    console.log(`[AUTH_INIT] resolved authType: ${authType}`);
    console.log(`[AUTH_INIT] displayName: ${displayName}`);

    const profile = await PlayerProfile.findOneAndUpdate(
      { uid },
      {
        $set: {
          displayName,
          authType,
          email: req.user.email || null,
          photoUrl: req.user.picture || null,
          lastActiveAt: new Date(),
          updatedAt: new Date()
        },
        $setOnInsert: {
          highestXP: 0,
          highestLevelReached: 1,
          lastLevelReached: 1,
          totalCompletions: 0,
          totalFailures: 0,
          totalAbandoned: 0,
          createdAt: new Date()
        }
      },
      { upsert: true, new: true, setDefaultsOnInsert: true }
    );

    console.log(`[AUTH_INIT] Profile synced for ${uid}. authType: ${authType}`);
    res.status(200).json({ message: 'Profile initialized successfully', data: profile });
  } catch (error) {
    console.error('[AUTH_INIT] Error initializing profile:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

module.exports = router;
