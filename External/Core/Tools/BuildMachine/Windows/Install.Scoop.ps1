# filename: Install.Scoop.ps1 (can be run via powershell)

# install scoop
set-executionpolicy remotesigned -scope currentuser

# confirm with A (all)
iwr -useb get.scoop.sh | iex
