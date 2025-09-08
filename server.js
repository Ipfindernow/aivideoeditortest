const express = require('express');
const multer = require('multer');
const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const app = express();
const upload = multer({ dest: 'uploads/' });
app.use(express.json());

app.post('/api/analyze', upload.array('clips'), (req, res) => {
  const files = req.files;

  // Validation: at least 4 clips
  if (!files || files.length < 4) {
    return res
      .status(400)
      .send(`Please upload at least 4 clips. You sent ${files ? files.length : 0}.`);
  }

  const pyScript = path.join(__dirname, 'analyze_clips.py');
  const args = files.map(f => f.path);

  const out = spawnSync('python3', [pyScript, ...args], {
    encoding: 'utf8',
    maxBuffer: 20 * 1024 * 1024,
  });

  if (out.error) {
    cleanupFiles(files);
    return res.status(500).send('Server error: ' + out.error.message);
  }
  if (out.status !== 0) {
    cleanupFiles(files);
    console.error('Python error:', out.stderr);
    return res.status(500).send('Analysis failed: ' + out.stderr);
  }

  let json;
  try {
    json = JSON.parse(out.stdout);
  } catch (err) {
    cleanupFiles(files);
    return res.status(500).send('Failed to parse analysis output');
  }

  cleanupFiles(files);
  return res.json(json);
});

function cleanupFiles(files) {
  files.forEach(f => {
    try { fs.unlinkSync(f.path); } catch (err) {}
  });
}

app.listen(3000, () => console.log('✅ Backend running on http://localhost:3000'));
