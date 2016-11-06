App.learning = App.cable.subscriptions.create "LearningChannel",
  connected: ->
    # Called when the subscription is ready for use on the server

  disconnected: ->
    # Called when the subscription has been terminated by the server

  # Websocket通信でサーバから値を受信した時に動作
  received: (data) ->
    # デバック用
    console.log(data)
    # 受信データに接続数が含まれている場合
    if data["connected_count"] >= 0
      # 接続数を更新
      $(".connected-count").text(data["connected_count"])
      return
    # 受信データに理解度の値が含まれている場合
    else if data["expression_data"]
      # jsonオブジェクトに変換
      expressionAsJson = $.parseJSON(data["expression_data"])
      # 生徒の表情から算出した理解度の値取得
      expression = parseInt(expressionAsJson.smile)

      # 更新する顔画像のパス
      imgPath = "assets/"
      if expression > 80
        imgPath += "80_.png"
      else if expression > 60
        imgPath += "60_.png"
      else if expression > 40
        imgPath += "40_.png"
      else if expression > 20
        imgPath += "20_.png"
      else
        imgPath += "0_.png"
      # 顔画像を更新
      $(".expression-img").attr("src", imgPath)

      # TODO グラフの更新
      $(".expression-avg").text(expression.toFixed(1))
      return
    # 受信メッセージが通常のチャットメッセージの場合
    else
      # メッセージを表示するためのliタグ生成
      $li = '<li class="media message list-item">
        <div class="media-body">
        <h5 class="media-heading">
        <span class="student-name">' + data["student_name"] + '</span>　<small>
        ' + data["time"] + '</small>
        </h5>
        <p>' + data["message"] + '</p>
        </div>
        </li>'
      # messagesに受信したmessageを末尾に追加
      $('#messages').append $li
      # メッセージの表示枠を最下部までスクロール
      $(".scroll-box").delay(100).animate {
        scrollTop: $(document).height()
      }
      return

  # messageをWebsocket通信でサーバに送信
  speak: (message) ->
    # data["message"]にmessageをセットし、サーバ側のチャンネルのspeakメソッドに送信
    @perform 'speak', message: message, name: $("#send-user-name").text()


# メッセージの送信
$("#send-message-form").on "submit", (event) ->
  # 入力文字が1文字以上の場合
  if $('[data-behavior~=learning_speaker]').val().length > 0
    # 入力文字をspeakメソッドに渡す
    App.learning.speak $('[data-behavior~=learning_speaker]').val()
    # デバッグ用
    console.log($('[data-behavior~=learning_speaker]').val() + "を送信")
    # 入力フォームを空にする
    $('[data-behavior~=learning_speaker]').val("")
  # ページがリロードされないようにするための記述
  return false
