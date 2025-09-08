import sys, json, subprocess, os
from scenedetect import detect, ContentDetector
import numpy as np
import librosa

def extract_audio(video_path, out_wav):
    subprocess.check_call(
        ['ffmpeg','-y','-i',video_path,'-vn','-acodec','pcm_s16le','-ar','44100','-ac','1',out_wav],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
    )

def compute_energy(wav_path, hop_length=1024, frame_length=2048):
    y, sr = librosa.load(wav_path, sr=None)
    S = librosa.feature.rms(y=y, frame_length=frame_length, hop_length=hop_length)[0]
    times = librosa.frames_to_time(range(len(S)), sr=sr, hop_length=hop_length)
    return times.tolist(), S.tolist()

if __name__ == "__main__":
    video_paths = sys.argv[1:]
    results = {'files': []}

    for vp in video_paths:
        scene_list = detect(vp, ContentDetector())
        shots = [{'start': float(s[0].get_seconds()), 'end': float(s[1].get_seconds())} for s in scene_list]

        wav = vp + ".wav"
        extract_audio(vp, wav)
        times, energy = compute_energy(wav)

        energy_arr = np.array(energy)
        climax_intervals = []
        if len(energy_arr) > 0:
            idx = np.argpartition(energy_arr, -3)[-3:]
            for i in idx:
                t = times[i]
                climax_intervals.append({
                    "start": max(0, t - 1.5),
                    "end": t + 1.5,
                    "score": float(energy_arr[i])
                })

        results['files'].append({
            'video': os.path.basename(vp),
            'shots': shots,
            'climax_intervals': climax_intervals
        })

        try: os.remove(wav)
        except: pass

    print(json.dumps(results))
