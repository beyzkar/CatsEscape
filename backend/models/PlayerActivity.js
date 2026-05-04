const mongoose = require('mongoose');

const playerActivitySchema = new mongoose.Schema({
  uid: { type: String, required: true },
  userName: { type: String },
  eventType: { type: String, required: true, enum: ['session_start', 'game_start', 'game_end', 'level_result'] },
  levelNumber: { type: Number, default: null },
  result: { type: String, default: null, enum: ['completed', 'failed', 'abandoned', null] },
  sessionId: { type: String, required: true },
  createdAt: { type: Date, default: Date.now }
});

module.exports = mongoose.model('PlayerActivity', playerActivitySchema);
