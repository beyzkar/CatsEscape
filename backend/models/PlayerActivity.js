const mongoose = require('mongoose');

const playerActivitySchema = new mongoose.Schema({
  uid: { type: String, required: true },
  eventType: { type: String, required: true, enum: ['session_start', 'game_start', 'game_end'] },
  levelNumber: { type: Number, default: null },
  result: { type: String, default: null, enum: ['completed', 'failed', null] },
  sessionId: { type: String, required: true },
  createdAt: { type: Date, default: Date.now }
});

module.exports = mongoose.model('PlayerActivity', playerActivitySchema);
