# torrent-ratio-booster
Easily boost your torrent ratio.  This acts as a proxy server.  Configure your torrent client to point to the running app/container.

Example docker compose usage:
```
services:
    torrent-ratio-booster:
        container_name: torrent-ratio-booster
        image: ghcr.io/issafram/torrent-ratio-booster:main
        restart: unless-stopped
        ports:
            - "34555:34555/tcp"
        environment:
            PORT: 34555
            RATIO: 2.0
```