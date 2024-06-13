# get every file ending in .cs that exists within this directory or any subdirectory, and store the filepath in a list

import os

def getFiles():
    files = []
    for root, dirs, filenames in os.walk("."):
        for filename in filenames:
            if filename.endswith(".cs"):
                files.append(os.path.join(root, filename))
    return files

def countLines(files):
    totalLines = 0
    for file in files:
        with open(file, 'r') as f:
            lines = f.readlines()
            totalLines += len(lines)
    return totalLines

def main():
    files = getFiles()
    totalLines = countLines(files)
    print("Total lines of code: " + str(totalLines))
    
if __name__ == "__main__":
    main()