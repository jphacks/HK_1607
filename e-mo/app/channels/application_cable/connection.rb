module ApplicationCable
  class Connection < ActionCable::Connection::Base
    # コネクションの識別子
    identified_by :current_user

    # 自サイトのドメインにアクセスされた時呼ばれるメソッド
    def connect
      # カレントユーザ取得
      self.current_user = find_verified_user

      # カレントユーザが先生ではない場合(生徒だった場合)
      if !self.current_user.teacher_flag
        # ユーザの接続フラグをオンに
        user_connected
        # 接続数の通知
        ConnectCountJob.perform_later
      end
    end

    protected
      # アクセスしているユーザを返す
      def find_verified_user
        if current_user = User.find(cookies.signed[:user_id])
          current_user
        else
          reject_unauthorized_connection
        end
      end

      # ユーザの接続フラグをオンに
      def user_connected
        p "ユーザ接続！"
        user = User.find(current_user.id)
        user.connected_flag = true
        p user
        user.save
      end
  end
end
