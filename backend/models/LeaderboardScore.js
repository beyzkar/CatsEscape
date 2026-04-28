const mongoose = require('mongoose');

const leaderboardScoreSchema = new mongoose.Schema({
  uid: { type: String, required: true },
  displayName: { type: String, default: 'Player' },
  authType: { type: String, enum: ['google', 'guest'], default: 'guest' },
  levelNumber: { type: Number, required: true },
  score: { type: Number, required: true },
  xpEarned: { type: Number, default: 0 },
  timeSeconds: { type: Number, default: 0 }
}, { 
  timestamps: true 
});

// Compound index to quickly find a player's best score for a specific level
leaderboardScoreSchema.index({ uid: 1, levelNumber: 1 }, { unique: true });

module.exports = mongoose.model('LeaderboardScore', leaderboardScoreSchema);
