const admin = require('firebase-admin');

// Initialize Firebase Admin (this should be called once in server.js, but we'll add a check here too)
const verifyToken = async (req, res, next) => {
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({ message: 'Unauthorized: No token provided' });
  }

  const idToken = authHeader.split('Bearer ')[1];

  try {
    const decodedToken = await admin.auth().verifyIdToken(idToken);
    req.user = decodedToken; // uid is in decodedToken.uid
    next();
  } catch (error) {
    console.error('Error verifying Firebase token:', error);
    return res.status(403).json({ message: 'Unauthorized: Invalid token' });
  }
};

module.exports = verifyToken;
