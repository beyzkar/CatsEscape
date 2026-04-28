const mongoose = require('mongoose');

const levelResultSchema = new mongoose.Schema({
  uid: { type: String, required: true, index: true },
  levelNumber: { type: Number, required: true },
  levelResult: { type: String, enum: ['completed', 'failed', 'abandoned'], required: true },
  xpEarned: { type: Number, default: 0 },
  fishSpawnCount: { type: Number, default: 0 },
  potionSpawnCount: { type: Number, default: 0 },
  heartsGained: { type: Number, default: 0 },
  heartsLost: { type: Number, default: 0 },
  
  // Timing fields
  startedAt: { type: Date },
  completedAt: { type: Date },
  abandonedAt: { type: Date },
  durationSeconds: { type: Number },

  deviceInfo: {
    model: String,
    operatingSystem: String,
    language: String,
    country: String
  }
}, { timestamps: true });

module.exports = mongoose.model('LevelResult', levelResultSchema);
