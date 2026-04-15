#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# SSL-Zertifikat mit Let's Encrypt einrichten
# Voraussetzung: Domain zeigt bereits auf Server-IP (DNS A-Record)
# ═══════════════════════════════════════════════════════════════

set -e

if [ -z "$1" ]; then
    echo "Verwendung: ./setup-ssl.sh deine-domain.de"
    exit 1
fi

DOMAIN=$1

echo "→ Certbot wird installiert..."
apt install -y certbot python3-certbot-nginx

echo "→ Nginx-Config wird auf Domain $DOMAIN aktualisiert..."
sed -i "s/server_name _;/server_name $DOMAIN;/" /etc/nginx/sites-available/projektsoftware
nginx -t && systemctl reload nginx

echo "→ SSL-Zertifikat wird erstellt..."
certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos --email admin@"$DOMAIN"

echo ""
echo "══════════════════════════════════════════════════"
echo "  ✅ SSL eingerichtet!"
echo "  Deine App ist jetzt erreichbar unter:"
echo "  https://$DOMAIN"
echo "══════════════════════════════════════════════════"
