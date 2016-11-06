class LearningChannel < ApplicationCable::Channel

  # Connection.connetの後に呼ばれる
  def subscribed
    stream_from "learning_channel"
    stream_for current_user
    # カレントユーザが先生でない場合(生徒だった場合)接続数を先生に通知
    ConnectCountJob.perform_later if current_user.teacher_flag
  end

  def unsubscribed
    # 切断したユーザが先生ではない場合(生徒だった場合)
    if !self.current_user.teacher_flag
      # ユーザの接続フラグをオフに
      user_disconnected
      # 接続数の通知
      ConnectCountJob.perform_later
    end
  end

  def speak(data)
    # 登録されている先生全てに対する処理
    User.where(teacher_flag: true).each do |teacher|
      # メッセージ送信
      LearningChannel.broadcast_to(teacher, {
        student_name: current_user.student_name, message: data["message"], time: Time.now.strftime("%Y年%m月%d日 %H時%M分%S秒")
      })
    end
    # 自分宛てにメッセージ送信
    LearningChannel.broadcast_to(current_user, {
      student_name: current_user.student_name, message: data["message"], time: Time.now.strftime("%Y年%m月%d日 %H時%M分%S秒")
    })
  end

  protected
    # ユーザの接続フラグをオフに
    def user_disconnected
      user = User.find(current_user.id)
      user.connected_flag = false
      user.save
    end
end
