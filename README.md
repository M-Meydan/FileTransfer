# FileTransfer
## Development config: .Net 6.0

## Running Application takes 2 commands:
	1- *transfer* {sourceFolderPath} {destinationFolderPath}  // to transfers files from source folder to destination folder.
	  e.g. transfer ./ c:/temp/test
	  
	2- *exit* // to stop file transfer and exit.


## Application flow:

	While exit command not requested
		- user enters transfer command with source and destination folders
		- if command and folder paths valid
			- user can add another command (async operation through MediatR command event)
				if command exit then stop application
			- Create queue for each file type
			- Create async consumer to process messages for each queue
			- Enqueue messages for each queue created
			
		- Otherwise error message displayed for errors
	
	
