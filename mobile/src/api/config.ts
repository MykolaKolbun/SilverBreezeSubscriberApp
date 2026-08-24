// Backend base URL.
//
// Default points at the deployed API via the Cloudflare tunnel hostname you add
// for the API (sweb.alternatiview.com.ua -> localhost:8085 on the Pi).
// For local testing against a dev server, replace with your machine's LAN IP,
// e.g. 'http://192.168.1.50:5080' (a phone can't reach 'localhost').
export const API_BASE_URL = 'https://sweb.alternatiview.com.ua';
