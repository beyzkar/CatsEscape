const mongoose = require('mongoose');

const leaderboardScoreSchema = new mongoose.Schema({
  uid: { type: String, required: true },
  userName: { type: String },
  displayName: { type: String, default: 'Player' },
  authType: { type: String, enum: ['google', 'guest'], default: 'guest' },
  levelNumber: { type: Number, required: true },
  score: { type: Number, required: true },
  xpEarned: { type: Number, default: 0 },
  timeSeconds: { type: Number, default: 0 }
}, {
  timestamps: true
});

// Non-unique index for fast lookups
leaderboardScoreSchema.index({ levelNumber: 1, score: -1 });

module.exports = mongoose.model('LeaderboardScore', leaderboardScoreSchema);
