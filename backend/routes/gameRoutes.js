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
        highestXP: xpEarned,
        totalCompletions: isCompleted ? 1 : 0,
        totalFailures: isFailed ? 1 : 0,
        totalAbandoned: isAbandoned ? 1 : 0
      },
      lastActiveAt: new Date()
    };

    if (userName) updateData.$set = { userName };

    if (isCompleted) {
      updateData.lastLevelReached = levelNumber + 1;
      updateData.$max = { highestLevelReached: levelNumber + 1 };
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

    if (levelNumber === undefined || score === undefined) {
      return res.status(400).json({ message: 'levelNumber and score are required' });
    }

    const existingEntry = await LeaderboardScore.findOne({ uid, levelNumber });

    if (existingEntry) {
      if (score > existingEntry.score) {
        existingEntry.score = score;
        existingEntry.displayName = displayName || existingEntry.displayName;
        existingEntry.authType = authType || existingEntry.authType;
        existingEntry.userName = userName || existingEntry.userName;
        existingEntry.xpEarned = xpEarned !== undefined ? xpEarned : existingEntry.xpEarned;
        existingEntry.timeSeconds = timeSeconds !== undefined ? timeSeconds : existingEntry.timeSeconds;
        await existingEntry.save();
        console.log(`[Backend] Updated best score for user ${uid}, level ${levelNumber}: ${score}`);
      }
      return res.status(200).json({ message: 'Score processed', data: existingEntry });
    } else {
      const newScore = new LeaderboardScore({
        uid,
        userName,
        displayName: displayName || 'Player',
        authType: authType || 'guest',
        levelNumber,
        score,
        xpEarned: xpEarned || 0,
        timeSeconds: timeSeconds || 0
      });
      await newScore.save();
      console.log(`[Backend] Saved new best score for user ${uid}, level ${levelNumber}: ${score}`);
      return res.status(201).json({ message: 'Score saved successfully', data: newScore });
    }
  } catch (error) {
    console.error('[Backend] Error saving score:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

// GET /api/game/leaderboard
router.get('/leaderboard', verifyToken, async (req, res) => {
  try {
    const { levelNumber, limit = 10, sortBy = 'score' } = req.query;

    let query = {};
    if (levelNumber) {
      query.levelNumber = parseInt(levelNumber);
    }

    let sortOption = {};
    if (sortBy === 'timeSeconds') {
      sortOption[sortBy] = 1;
    } else {
      sortOption[sortBy] = -1;
    }

    const entries = await LeaderboardScore.find(query)
      .sort(sortOption)
      .limit(parseInt(limit));

    res.status(200).json({ success: true, entries });
  } catch (error) {
    console.error('[Backend] Error fetching leaderboard:', error);
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

    // Check if username is already taken
    const existing = await PlayerProfile.findOne({
      userName: { $regex: new RegExp(`^${trimmedName}$`, 'i') }
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
