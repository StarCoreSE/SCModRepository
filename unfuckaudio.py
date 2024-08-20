import os
import sys
import subprocess
from pathlib import Path
import uuid


def get_user_confirmation(prompt, default="y"):
    valid = {"yes": True, "y": True, "ye": True, "no": False, "n": False}
    if default == "y":
        prompt += " [Y/n] "
    elif default == "n":
        prompt += " [y/N] "
    else:
        raise ValueError(f"Invalid default value: '{default}'")

    while True:
        choice = input(prompt).lower()
        if choice == '':
            return valid[default]
        elif choice in valid:
            return valid[choice]
        else:
            print("Please respond with 'yes' or 'no' (or 'y' or 'n').")

def check_audio_sample_rate(file_path):
    try:
        result = subprocess.run(
            ['ffprobe', '-v', 'error', '-select_streams', 'a:0', '-show_entries', 'stream=sample_rate', '-of', 'default=noprint_wrappers=1:nokey=1', str(file_path)],
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True
        )
        return int(result.stdout.strip())
    except (ValueError, subprocess.CalledProcessError):
        return None  # Return None if we can't determine the sample rate

def convert_audio(input_file, output_file):
    try:
        if input_file == output_file:
            # Generate a temporary filename
            temp_output = output_file.with_stem(f"{output_file.stem}_{uuid.uuid4().hex[:8]}_temp")
        else:
            temp_output = output_file

        command = [
            'ffmpeg',
            '-y',  # Overwrite output files without asking
            '-i', str(input_file),
            '-acodec', 'pcm_s16le',
            '-ar', '44100',
            str(temp_output)
        ]
        subprocess.run(command, check=True, stderr=subprocess.PIPE, text=True)

        if input_file == output_file:
            # Replace the original file with the temp file
            os.remove(input_file)
            os.rename(temp_output, output_file)

        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ FFmpeg error converting {input_file.name}: {e.stderr.strip()}")
    except FileNotFoundError:
        print("❌ FFmpeg is not installed or not in system PATH. Please install FFmpeg to use this script.")
        sys.exit(1)
    except PermissionError:
        print(f"❌ Permission denied when trying to replace {input_file.name}. Please check file permissions.")
    except Exception as e:
        print(f"❌ Unexpected error converting {input_file.name}: {e}")

    return False
def process_directory():
    current_dir = Path.cwd()
    print(f"🔍 Processing audio files in: {current_dir}\n")

    if not get_user_confirmation("Do you want to start the conversion process?"):
        print("Process aborted by user.")
        return

    replace_originals = get_user_confirmation("\nReplace original files with new .wav files? If no, both will be kept.",
                                              default="n")
    print(f"\n{'Replacing' if replace_originals else 'Keeping'} original files.\n")

    total_files = 0
    successful = 0
    failed = 0
    skipped = 0
    audio_extensions = ('.mp3', '.m4a', '.flac', '.ogg', '.aac', '.wav', '.wma', '.opus')

    for file_path in current_dir.rglob('*'):
        if file_path.suffix.lower() in audio_extensions:
            total_files += 1
            output_path = file_path.with_suffix('.wav')

            print(f"\n🔄 Processing: {file_path.relative_to(current_dir)}")

            if file_path.stat().st_size == 0:
                print(f"⚠️ Skipping {file_path.name} (empty file)")
                failed += 1
                continue

            if file_path.suffix.lower() == '.wav':
                sample_rate = check_audio_sample_rate(file_path)
                if sample_rate == 44100:
                    print(f"✅ {file_path.name} is already a 44100Hz WAV. Skipping.")
                    skipped += 1
                    continue

            if convert_audio(file_path, output_path):
                if replace_originals:
                    if file_path.suffix.lower() != '.wav' or not file_path.samefile(output_path):
                        os.remove(file_path)
                    print(f"✅ Converted and replaced with {output_path.name}")
                else:
                    print(f"✅ Converted. Kept both {file_path.name} and {output_path.name}")
                successful += 1
            else:
                print(f"❌ Conversion failed for {file_path.name}")
                failed += 1

    # Summary
    print(f"\n📊 Conversion Summary:")
    print(f"   Total audio files found: {total_files}")
    print(f"   Successfully converted:  {successful}")
    print(f"   Already correct format:  {skipped}")
    print(f"   Failed conversions:      {failed}")

if __name__ == "__main__":
    if sys.platform == 'win32':
        sys.stdout.reconfigure(encoding='utf-8')

    try:
        process_directory()
    except KeyboardInterrupt:
        print("\n\n❗ Process interrupted by user. Exiting gracefully.")
        sys.exit(0)