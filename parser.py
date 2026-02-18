import sys
import re
import json
"""
This script parses a log file to extract the maximum tensile stress value (S11 Max) from a specific line format. It looks for lines containing "Model S11 Max (Tensile)" and extracts the numerical value following the "=" sign. The result is printed in JSON format (intended to further use in SQL database), indicating success or failure of the parsing operation.
"""
def parse_log(file_path):
    s11_value = 0.0
    found = False

    try:
        with open(file_path, 'r') as f:
            for line in f:
                # We look for: Model S11 Max (Tensile)    = 546.3
                if "Model S11 Max (Tensile)" in line:
                    
                    parts = line.split('=')
                    if len(parts) >= 2:
                        
                        # Example part: " 546.3 ------>>  [  OK  ]"
                        raw_value = parts[3].split('-')[0].strip() # Get 546.3
                        s11_value = float(raw_value)
                        found = True
                        break # Stop after finding the first one 
    
        
        result = {
            "file": file_path,
            "s11_max": s11_value,
            "status": "Success" if found else "NotFound"
        }
        print(json.dumps(result))

    except Exception as e:
        print(json.dumps({"status": "Error", "message": str(e)}))

if __name__ == "__main__":
    
    if len(sys.argv) > 1:
        parse_log(sys.argv[1])
    else:
        print(json.dumps({"status": "Error", "message": "No file provided"}))