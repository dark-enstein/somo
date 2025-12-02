import sys
sys.path.append('ml/scripts')

from ml.scripts.record_gestures import GestureRecorder

gr = GestureRecorder("ml/data/raw")
gr.record("point", 500)