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

    if (isCompleted) {
      // If completed, set current "lastLevelReached" to levelNumber + 1
      updateData.lastLevelReached = levelNumber + 1;
      // Also update highestLevelReached using $max
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
    const { eventType, levelNumber, result, sessionId } = req.body;

    if (!eventType || !sessionId) {
      return res.status(400).json({ message: 'eventType and sessionId are required' });
    }

    const newActivity = new PlayerActivity({
      uid: req.user.uid,
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
    const { displayName, authType, levelNumber, score, xpEarned, timeSeconds } = req.body;
    const uid = req.user.uid;

    if (levelNumber === undefined || score === undefined) {
      return res.status(400).json({ message: 'levelNumber and score are required' });
    }

    // Best Score Logic: Find existing score for this user and level
    const existingEntry = await LeaderboardScore.findOne({ uid, levelNumber });

    if (existingEntry) {
      // Only update if the new score is better (higher)
      if (score > existingEntry.score) {
        existingEntry.score = score;
        existingEntry.displayName = displayName || existingEntry.displayName;
        existingEntry.authType = authType || existingEntry.authType;
        existingEntry.xpEarned = xpEarned !== undefined ? xpEarned : existingEntry.xpEarned;
        existingEntry.timeSeconds = timeSeconds !== undefined ? timeSeconds : existingEntry.timeSeconds;
        await existingEntry.save();
        console.log(`[Backend] Updated best score for user ${uid}, level ${levelNumber}: ${score}`);
      } else {
        console.log(`[Backend] New score (${score}) is not better than existing best (${existingEntry.score}) for user ${uid}, level ${levelNumber}`);
      }
      return res.status(200).json({ message: 'Score processed', data: existingEntry });
    } else {
      // Create new entry if none exists
      const newScore = new LeaderboardScore({
        uid,
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
      sortOption[sortBy] = 1; // Smallest time is better
    } else {
      sortOption[sortBy] = -1; // Highest score/xp is better
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
    const { displayName, email, photoUrl, authType } = req.body;
    const uid = req.user.uid;

    if (!authType) {
      return res.status(400).json({ message: 'authType is required' });
    }

    console.log(`[Backend] Profile Init Request for UID: ${uid}, AuthType: ${authType}`);
    
    // Upsert Profile: Update info but preserve progress if it already exists
    const profile = await PlayerProfile.findOneAndUpdate(
      { uid },
      {
        $set: {
          displayName,
          email,
          photoUrl,
          authType,
          lastActiveAt: new Date()
        },
        $setOnInsert: {
          highestXP: 0,
          highestLevelReached: 1,
          lastLevelReached: 1,
          totalCompletions: 0,
          totalFailures: 0,
          totalAbandoned: 0
        }
      },
      { upsert: true, new: true, setDefaultsOnInsert: true }
    );

    console.log(`[Backend] Profile upserted for user ${uid}. Resulting profile lastLevelReached: ${profile.lastLevelReached}`);
    res.status(200).json({ message: 'Profile initialized successfully', data: profile });
  } catch (error) {
    console.error('[Backend] Error initializing profile:', error);
    res.status(500).json({ message: 'Server error', error: error.message });
  }
});

module.exports = router;
