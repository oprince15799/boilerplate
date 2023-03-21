const https = require('https');
const http = require('http');
const fs = require('fs');
const next = require('next');

const dev = process.env.NODE_ENV !== 'production';
const app = next({ dev });
const handle = app.getRequestHandler();

const httpsOptions = {
  key: fs.readFileSync('localhost-key.pem'),
  cert: fs.readFileSync('localhost-cert.pem'),
};

app.prepare().then(() => {
  // Create HTTPS server
  https.createServer(httpsOptions, (req, res) => {
    handle(req, res);
  }).listen(3000, () => {
    console.log(`> Ready on https://localhost:3000`);
    console.log(`> Ready on http://localhost:3001`);
  });

  // Create HTTP server and redirect to HTTPS
  http.createServer((req, res) => {
    res.writeHead(301, { Location: `https://localhost:3000${req.url}` });
    res.end();
  }).listen(3001);
});
