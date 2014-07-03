# Export and sync M-Files data to database  

## Quick start

* Install service `Robb`:
	
	`$> Robb.exe install --sudo` 

* Start service `Robb`:
	
	`$> Robb.exe start --sudo`

* Look to the `Event Log` for messages with source `Robb`
* Uninstall service:
	
	`$> Robb.exe uninstall` 

## Configuration

* `Robb.exe.config` App settings 
	* interval --- Integer. Program check M-Files updates each	`interval` minute(s)
* `ConnectionString.config` (not in repository) Database connection 

	```xml
	<connectionStrings>
	  <add name="DocumentsContext" connectionString="Data Source=(LocalDB)\v11.0;AttachDbFilename=C:/path/to/db.mdf;Integrated Security=True;Connect Timeout=30" providerName="System.Data.SqlClient"/>
	</connectionStrings>
	```
* `Private.config` (not in repository) M-Files account 

	```xml
	<appSettings>
	  <add key="MFilesHost" value="host.com" />
	  <add key="MFilesUser" value="" />
	  <add key="MFilesPassword" value="" />
	</appSettings>
	```


* `settings.json` Configuring the processing vaults (Conventions) and properties mapping between M-Files and Database 

## Command line options (from TopShelf library)

The help text from the command line is shown below for easy reference.

`Robb.exe [verb] [-option:value] [-switch]`

* **run** Runs the service from the command line (default)
* **help** or –help Displays help

* **install** Installs the service
	* -username	The username to run the service
	* -password	The password for the specified username
	* -instance	An instance name if registering the service multiple times
	* --autostart	The service should start automatically (default)
	* --disabled	The service should be set to disabled
	* --manual	The service should be started manually
	* --delayed	The service should start automatically (delayed)
	* --localsystem	Run the service with the local system account
	* --localservice	Run the service with the local service account
	* --networkservice 	Run the service with the network service permission
	* --interactive	The service will prompt the user at installation for the service credentials
	* --sudo	Prompts for UAC if running on Vista/W7/2008
	* -servicename	The name that the service should use when installing
	* -description	The service description the service should use when installing
	* -displayname	The display name the the service should use when installing
* **start** Starts the service if it is not already running
	* -instance	The instance to start
* **stop** Stops the service if it is running
	* -instance	The instance to stop
* **uninstall** Uninstalls the service
	* -instance	An instance name if registering the service multiple times
	* --sudo	Prompts for UAC if running on Vista/W7/2008