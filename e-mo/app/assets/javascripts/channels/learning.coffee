App.learning = App.cable.subscriptions.create "LearningChannel",
  connected: ->
    # Called when the subscription is ready for use on the server

  disconnected: ->
    # Called when the subscription has been terminated by the server

  # TODO 処理をメソッドに分けていく
  received: (data) ->
    console.log(data)
    if data["connected_count"] >= 0
      # console.log("接続数の情報")
      $(".connected-count").text(data["connected_count"])
      return
    else if data["expression_data"]
      # console.log("表情の値")

      # jsonオブジェクトに変換
      expressionAsJson = $.parseJSON(data["expression_data"])

      # TODO smile他のデータから平均値を算出してexpressionを生成
      expression = expressionAsJson.smile

      # TODO 画像の置き換え, herokuで動くか心配
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
      $(".expression-img").attr("src", imgPath)

      # TODO グラフの描写
      return
    else
      # console.log("メッセージ受信")
      # messagesに受信したmessageをセット
      $li = '<li class="media message list-item">
        <div class="media-body">
        <h5 class="media-heading">
        <span class="student-name">' + data["student_name"] + '</span>　<small>
        ' + data["time"] + '</small>
        </h5>
        <p>' + data["message"] + '</p>
        </div>
        </li>'
      $('#messages').append $li
      $(".scroll-box").delay(100).animate {
        scrollTop: $(document).height()
      }
      return

  speak: (message) ->
    # data["message"]にmessageをセットし、サーバ側のチャンネルのspeakメソッドに送信
    @perform 'speak', message: message, name: $("#send-user-name").text()

# 生徒側のメッセージが送信される
$("#send-message-form").on "submit", (event) ->
  if $('[data-behavior~=learning_speaker]').val().length > 0
    App.learning.speak $('[data-behavior~=learning_speaker]').val()
    console.log($('[data-behavior~=learning_speaker]').val() + "を送信")
    $('[data-behavior~=learning_speaker]').val("")
  return false
