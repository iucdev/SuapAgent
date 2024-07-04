
# SuapAgent
Данный репозиторий содержит шаблон кода, который собирает данные с КПУ и отправляет их в API сервис.
Вы можете использовать данный шаблон с "коробки" или доработать его под свои задачи.


- Основная ссылка для разработчиков: https://alcotrack.qoldau.kz/ru/dev-docs
- Ссылка тестового AlcoTrack Api: https://demo-alcotrack-api.qoldau.kz/swagger/index.html#/Api
- Ссылка основного AlcoTrack Api: https://alcotrack-api.qoldau.kz/swagger/index.html#/Api

Для билда
- `dotnet publish qoldau.suap.miniagent.csproj -r win-x64 -c release`
- SuapAgent\bin\release\net6.0\win-x64\qoldau.suap.miniagent.exe