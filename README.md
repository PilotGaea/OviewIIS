# OviewIIS
讓O'view Map Server與IIS整合

+ v1(done) : 最粗淺的整合，第一次地形連線時會驗證O'view MapServer是否啟動，但還是透過http與O'view Map Server連線
	+ TMPEngine若掉參考請重新加入。
	+ 預設O'view Map Server 使用 8080 port
	+ 預設專案路徑為 C:\ProgramData\PilotGaea\PGMaps\地圖伺服器#01\Map.TMPX 
	+ 專案中須有名為terrain的地形圖層
+ v2 : 試圖不透過http而直接呼叫CGeoDatabase
+ v3 : 界接其他web server如 apache、nginx
