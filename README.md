
# SuapAgent
Данный репозиторий содержит шаблон кода, который собирает данные с КПУ и отправляет их в API сервис.
Вы можете использовать данный шаблон с "коробки" или доработать его под свои задачи.


- Основная ссылка для разработчиков: https://alcotrack.qoldau.kz/ru/dev-docs
- Ссылка тестового AlcoTrack Api: https://demo-alcotrack-api.qoldau.kz/swagger/index.html#/Api
- Ссылка основного AlcoTrack Api: https://alcotrack-api.qoldau.kz/swagger/index.html#/Api

Для билда windows 64-бит
- `dotnet publish qoldau.suap.miniagent.csproj -r win-x64 -c release`
- SuapAgent\bin\release\net6.0\win-x64\qoldau.suap.miniagent.exe

Для билда windows 32-бит
- `dotnet publish qoldau.suap.miniagent.csproj -r win-x86 -c release --self-contained true`
- SuapAgent\bin\release\net6.0\win-x86\qoldau.suap.miniagent.exe



Название табличек и необходимых полей в AlcoTrack:
	
	tbAccColumnStateCounter  (по температуре колонны)
		- Temperature
		- StampDate

	tbAccReservoirCounter (по уровню)
		- ReservoirLevel
		- Weight
		- Volume
		- Density
		- Temperature
		- Fortress
		- StampDate
		
	tbAccProductCounter (по произведенному продукту)
		- Fortress
		- Volume
		- VolumeOfAlcohol
		- Temperature
		- Density
		- StampDate
		
		
	tbAccBeerCounter (по пиву)
		- Volume
		- VolumeBeer
		- ImidiateVolume
		- Temperature
		- Density
		- Conductivity
		- Fortress
		- StampDate

		
```		
|--------------------------------|
| тип данных     |  кол-во байт  |
|--------------------------------|
| Byte		 |	1    	 |
| UInt16	 |	2   	 |
| Int16		 |	2        |
| UInt32	 |	4        |
| Int32		 |	4        |
| Single	 |	4        |
| Float		 |	4        |
| Double	 |	8        |
| UInt64	 |	8        |
| Int64		 |	8        |
| DatetimeS7 	 |	12       |
|--------------------------------|
```

```
	sc.exe delete "Qoldau Alcotrack Agent Service"
	sc.exe create "Qoldau Alcotrack Agent Service" binpath= "D:\Work\SuapAgent\bin\release\net6.0\win-x64\qoldau.suap.miniagent.exe D:\Work\SuapAgent\bin\release\net6.0\win-x64" start=auto
	sc.exe failure "Qoldau Alcotrack Agent Service" reset= 0 actions= restart/0/restart/0/restart/0
```