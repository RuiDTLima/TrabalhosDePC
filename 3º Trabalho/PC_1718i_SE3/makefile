##
# Used to generate server and test client executables.
# With current directory in the directory where this makefile resides execute: nmake
# That command generates two executable: (Server\Server.exe and Client\Client.ex)
##

all: Server.exe Client.exe
	
Server.exe : Server\Server.cs Server\Store.cs Server\Logger.cs
	csc /debug+ /out:Server/Server.exe Server\Server.cs Server\Store.cs Server\Logger.cs
	
Client.exe : Client\Client.cs
	csc /debug+ /out:Client/Client.exe Client\Client.cs
	
clean:
	@del Server\Server.exe Client\Cliente.exe 2>NUL
  