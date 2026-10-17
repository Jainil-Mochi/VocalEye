import subprocess
import shlex

def execute_command(command):
    try:
        
        subprocess.run(shlex.split(command), check=True,shell=True)
        return "Command executed successfully."
    except Exception as e:
        return "Error executing command"

