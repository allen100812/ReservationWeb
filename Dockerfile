# -------- 第一階段：建置專案（Build） --------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# 複製所有檔案進入 /src
COPY . .

# 執行 dotnet publish，編譯為 Release 模式，輸出到 /app/publish
RUN dotnet publish -c Release -o /app/publish


# -------- 第二階段：執行環境（Runtime） --------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# 複製已發佈的內容
COPY --from=build /app/publish .

# ✅ 加入 wait-for-it.sh 腳本（這才是會執行的容器）
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

# 開放 port 80
EXPOSE 80

# ✅ 啟動前先等待資料庫準備好
ENTRYPOINT ["/wait-for-it.sh", "db:3306", "--timeout=30", "--", "dotnet", "Web0524.dll"]
