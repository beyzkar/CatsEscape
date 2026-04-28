const mongoose = require('mongoose');

const playerProfileSchema = new mongoose.Schema({
  uid: { type: String, required: true, unique: true },
  email: { type: String },
  displayName: { type: String },
  photoUrl: { type: String },
  authType: { type: String, enum: ['google', 'guest'], default: 'guest' },
  highestXP: { type: Number, default: 0 },
  highestLevelReached: { type: Number, default: 1 },
  lastLevelReached: { type: Number, default: 1 },
  totalCompletions: { type: Number, default: 0 },
  totalFailures: { type: Number, default: 0 },
  totalAbandoned: { type: Number, default: 0 },
  lastActiveAt: { type: Date, default: Date.now }
}, { timestamps: true });

module.exports = mongoose.model('PlayerProfile', playerProfileSchema);
