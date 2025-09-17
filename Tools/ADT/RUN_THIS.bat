@echo off
if exist entity_ids.txt del entity_ids.txt
if exist MapMissingProtos.txt del MapMissingProtos.txt

REM Убедитесь, что библиотеки установлены
python LibCheck.py
python -m pip install --disable-pip-version-check --no-input --quiet pyyaml

python ProtoCollector.py
python MapsProtoTargeter.py
