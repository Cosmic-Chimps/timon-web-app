#!/usr/bin/env bash

docker build . -t jjchiw/timon-web-app:$(git rev-parse --short HEAD)
docker push jjchiw/timon-web-app:$(git rev-parse --short HEAD)
git rev-parse --short HEAD
