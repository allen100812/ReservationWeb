# -------- 第一階段：建置專案（Build） --------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# 複製所有檔案進入容器中的 /src 資料夾
COPY . .

# 執行 dotnet publish，編譯成 Release 模式並輸出到 /app/publish
RUN dotnet publish -c Release -o /app/publish


# -------- 第二階段：執行環境（Runtime） --------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# 將上一步 build 出來的檔案複製到執行容器中
COPY --from=build /app/publish .

# 開放 port 80 給外部使用
EXPOSE 80

# 加入 wait-for-it 腳本
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

# 改用 wait-for-it 包住 Web 啟動（等待 db:3306 準備好）
#ENTRYPOINT ["/wait-for-it.sh", "db:3306", "--", "dotnet", "Web0524.dll"]
#
# 改成這樣（暫時保留容器活著給你進入）
CMD ["sleep", "999999"]