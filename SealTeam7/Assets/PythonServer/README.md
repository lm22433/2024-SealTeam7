## Setup
1. Set up a virtualenv in `Assets/PythonServer/.venv`. It's important for it to be in `.venv` rather than `venv` so that Unity doesn't try to load the libraries. Tested with Python 3.11. MEDIAPIPE IS NOT YET AVAILABLE FOR PYTHON 3.13.
2. Activate the virtualenv. Bash: `source .venv/bin/activate`; PowerShell: `.\.venv\Scripts\Activate.ps1`
3. Install requirements: `pip install -r requirements.txt`
4. Start server: `python main.py`